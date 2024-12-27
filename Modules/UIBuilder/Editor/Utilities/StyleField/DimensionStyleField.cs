// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;
using UnityEngine.UIElements.StyleSheets.Syntax;

namespace Unity.UI.Builder
{
    internal class DimensionStyleField : StyleField<float>
    {
        static readonly string k_DraggerFieldUssClassName = "unity-style-field__dragger-field";

        List<string> m_Units = new List<string>() { StyleFieldConstants.UnitPixel };
        static readonly string s_DefaultUnit = StyleFieldConstants.UnitPixel;

        [Serializable]
        public new class UxmlSerializedData : StyleField<float>.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                StyleField<float>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new DimensionStyleField();
        }

        static public string defaultUnit => s_DefaultUnit;

        IntegerField m_DraggerIntegerField;
        protected IntegerField draggerIntegerField => m_DraggerIntegerField;

        bool m_IsFieldDraggerInitialized;

        float m_DragStep = 1;

        public float dragStep
        {
            get
            {
                return m_DragStep;
            }

            set
            {
                if (m_DragStep == value)
                    return;
                m_DragStep = value;
                UpdateDragger();
            }
        }

        public List<string> units => m_Units;

        public float length
        {
            get => innerValue;
            set
            {
                innerValue = value;
                SetValueWithoutNotify(innerValue.ToString(CultureInfo.InvariantCulture.NumberFormat));
            }
        }

        public Dimension.Unit unit
        {
            get
            {
                var found = StyleFieldConstants.StringToDimensionUnitMap.TryGetValue(option, out var outUnit);
                if (found)
                    return outUnit;

                return Dimension.Unit.Unitless;
            }
            set
            {
                var found = StyleFieldConstants.DimensionUnitToStringMap.TryGetValue(value, out var opt);
                if (found)
                    option = opt;
                else
                    option = s_DefaultUnit;

                SetValueWithoutNotify(option);
            }
        }

        public Func<float, Dimension.Unit, Dimension.Unit, float> valueConverter { get; set; }

        public bool isUsingLabelDragger { get; private set; }

        public DimensionStyleField() : this(string.Empty) { }

        public DimensionStyleField(string label) : base(label)
        {
            m_DraggerIntegerField = new IntegerField(" ");
            m_DraggerIntegerField.name = "dragger-integer-field";
            m_DraggerIntegerField.AddToClassList(k_DraggerFieldUssClassName);
            m_DraggerIntegerField.focusable = false;
            m_DraggerIntegerField.RegisterValueChangedCallback(OnDraggerFieldUpdate);
            m_DraggerIntegerField.labelElement.RegisterCallback<PointerUpEvent>(OnDraggerPointerUp);
            Insert(0, m_DraggerIntegerField);
            option = defaultUnit;

            RefreshChildFields();
        }

        void OnDraggerPointerUp(PointerUpEvent evt)
        {
            if (!isUsingLabelDragger)
                return;

            isUsingLabelDragger = false;

            // Sending change event on release after we reset the isUsingLabelDragger, so that we can trigger a full update.
            // The value didn't change but the state of this control in regards to the value did.
            using (var e = ChangeEvent<string>.GetPooled(value, value))
            {
                e.elementTarget = this;
                SendEvent(e);
            }
        }

        bool FindUnitInExpression(Expression expression, DataType dataType)
        {
            if (expression.type == ExpressionType.Data && expression.dataType == dataType)
                return true;

            if (expression.subExpressions == null)
                return false;

            foreach (var subExp in expression.subExpressions)
                if (FindUnitInExpression(subExp, dataType))
                    return true;

            return false;
        }

        protected override List<string> GenerateAdditionalOptions(string binding)
        {
            if (string.IsNullOrEmpty(binding))
                return m_Units;

            var syntaxParser = new StyleSyntaxParser();

            var syntaxFound = StylePropertyCache.TryGetSyntax(binding, out var syntax);
            if (!syntaxFound)
                return StyleFieldConstants.KLDefault;

            var expression = syntaxParser.Parse(syntax);
            if (expression == null)
                return StyleFieldConstants.KLDefault;

            var hasLength = FindUnitInExpression(expression, DataType.Length);
            var hasPercent = FindUnitInExpression(expression, DataType.Percentage);

            m_Units.Clear();
            if (hasLength)
                m_Units.Add(StyleFieldConstants.UnitPixel);
            if (hasPercent)
                m_Units.Add(StyleFieldConstants.UnitPercent);

            var hasTime = FindUnitInExpression(expression, DataType.Time);
            if (hasTime)
            {
                m_Units.Add(StyleFieldConstants.UnitSecond);
                m_Units.Add(StyleFieldConstants.UnitMillisecond);
            }
            return m_Units;
        }

        protected bool TryParseValue(string val, out float value)
        {
            var num = new string(val.Where((c) => Char.IsDigit(c) || c == '.' || c == '-').ToArray());
            return float.TryParse(num, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out value);
        }

        protected override bool SetInnerValueFromValue(string val)
        {
            if (styleKeywords.Contains(val))
                return false;

            if (!TryParseValue(val, out var length))
                return false;

            if (isKeyword)
                option = defaultUnit;

            innerValue = length;
            return true;
        }

        protected override bool SetOptionFromValue(string val)
        {
            if (base.SetOptionFromValue(val))
                return true;

            var unit = new string(val.Where((c) => !Char.IsDigit(c) && c != '.' && c != '-').ToArray());
            if (string.IsNullOrEmpty(unit) || !m_Units.Contains(unit))
                return false;

            var oldUnit = option;

            option = unit;

            // If only the unit was set then try to convert the inner value using the new unit and old unit
            if (!TryParseValue(val, out float result) &&
                (valueConverter != null) &&
                StyleFieldConstants.StringToDimensionUnitMap.TryGetValue(oldUnit, out var oldUnitEnum) &&
                StyleFieldConstants.StringToDimensionUnitMap.TryGetValue(unit, out var unitEnum))
            {
                innerValue = valueConverter.Invoke(innerValue, oldUnitEnum, unitEnum);
            }
            return true;
        }

        protected override string ComposeValue()
        {
            if (styleKeywords.Contains(option))
                return option;

            return innerValue.ToString(CultureInfo.InvariantCulture.NumberFormat) + option;
        }

        protected override string GetTextFromValue()
        {
            if (styleKeywords.Contains(option))
                return option;

            return innerValue.ToString(CultureInfo.InvariantCulture.NumberFormat);
        }

        protected override void RefreshChildFields()
        {
            if (!m_IsFieldDraggerInitialized)
            {
                var propName = GetProperty(BuilderConstants.InspectorStylePropertyNameVEPropertyName) as string;
                if (!string.IsNullOrEmpty(propName))
                {
                    m_IsFieldDraggerInitialized = true;
                    if (propName == "-unity-slice-scale")
                        dragStep = BuilderConstants.DimensionStyleFieldReducedDragStep;
                }
            }

            textField.SetValueWithoutNotify(GetTextFromValue());
            UpdateDragger();
            optionsPopup.SetValueWithoutNotify(GetOptionFromValue());
        }

        void UpdateDragger()
        {
            m_DraggerIntegerField?.SetValueWithoutNotify(Mathf.RoundToInt(innerValue / m_DragStep));
        }

        void OnDraggerFieldUpdate(ChangeEvent<int> evt)
        {
            isUsingLabelDragger = true;

            if (isKeyword)
                option = defaultUnit;

            value = (evt.newValue * m_DragStep).ToString(CultureInfo.InvariantCulture.NumberFormat);
            evt.StopImmediatePropagation();
        }
    }
}
