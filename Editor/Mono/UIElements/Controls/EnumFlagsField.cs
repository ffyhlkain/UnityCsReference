// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// Makes a dropdown for switching between enum flag values that are marked with the Flags attribute.
    /// </summary>
    /// <remarks>
    /// An option for the value 0 with name "Nothing" and an option for the value ~0 (that is, all bits set) with the
    /// name "Everything" are always displayed at the top of the menu. The names for the values 0 and ~0 can be
    /// overriden by defining these values in the enum type.
    ///
    /// For more information, refer to [[wiki:UIE-uxml-element-EnumFlagsField|UXML element EnumFlagsField]].
    /// </remarks>
    public class EnumFlagsField : BaseMaskField<Enum>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : BaseField<Enum>.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                BaseField<Enum>.UxmlSerializedData.Register();
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[]
                {
                    new (nameof(typeAsString), "type", typeof(Enum)),
                    new (nameof(valueAsString), "value"),
                    new (nameof(includeObsoleteValues), "include-obsolete-values"),
                });
            }

            #pragma warning disable 649
            [UxmlTypeReference(typeof(Enum))]
            [SerializeField, UxmlAttribute("type")] string typeAsString;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags typeAsString_UxmlAttributeFlags;
            [EnumFlagsFieldValueDecorator]
            [SerializeField, UxmlAttribute("value")] string valueAsString;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags valueAsString_UxmlAttributeFlags;
            [SerializeField] bool includeObsoleteValues;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags includeObsoleteValues_UxmlAttributeFlags;
            #pragma warning restore 649

            public override object CreateInstance() => new EnumFlagsField();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                var e = (EnumFlagsField)obj;
                if (ShouldWriteAttributeValue(includeObsoleteValues_UxmlAttributeFlags))
                    e.includeObsoleteValues = includeObsoleteValues;
                if (ShouldWriteAttributeValue(typeAsString_UxmlAttributeFlags))
                    e.typeAsString = typeAsString;
                if (ShouldWriteAttributeValue(valueAsString_UxmlAttributeFlags))
                    e.valueAsString = valueAsString;
            }
        }

        /// <summary>
        /// Instantiates a <see cref="EnumFlagsField"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        public new class UxmlFactory : UxmlFactory<EnumFlagsField, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="EnumFlagsField"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        public new class UxmlTraits : BaseMaskField<Enum>.UxmlTraits
        {
#pragma warning disable 414
            private UxmlTypeAttributeDescription<Enum> m_Type = EnumFieldHelpers.type;
            private UxmlStringAttributeDescription m_Value = EnumFieldHelpers.value;
            private UxmlBoolAttributeDescription m_IncludeObsoleteValues = EnumFieldHelpers.includeObsoleteValues;
#pragma warning restore 414

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                if (EnumFieldHelpers.ExtractValue(bag, cc, out var resEnumType, out var resEnumValue, out var resIncludeObsoleteValues))
                {
                    EnumFlagsField enumField = (EnumFlagsField)ve;
                    enumField.Init(resEnumValue, resIncludeObsoleteValues);
                }
                // If we didn't have a valid value, try to set the type.
                else if (null != resEnumType)
                {
                    EnumFlagsField enumField = (EnumFlagsField)ve;

                    enumField.m_EnumType = resEnumType;
                    if (enumField.m_EnumType != null)
                        enumField.PopulateDataFromType(enumField.m_EnumType);
                    enumField.value = null;
                }
                else
                {
                    var enumField = (EnumFlagsField)ve;
                    enumField.m_EnumType = null;
                    enumField.value = null;
                }
            }
        }

        /// <summary>
        /// USS class name for elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-enum-flags-field";
        /// <summary>
        /// USS class name for labels of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name for input elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

        private Type m_EnumType;
        private bool m_IncludeObsoleteValues;
        private EnumData m_EnumData;


        // These properties exist so that the UIBuilder can read them.
        internal Type type
        {
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            get => m_EnumType;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal bool includeObsoleteValues
        {
            get => m_IncludeObsoleteValues;
            set => m_IncludeObsoleteValues = value;
        }

        internal string typeAsString
        {
            get => UxmlUtility.TypeToString(m_EnumType);
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            set
            {
                m_EnumType = UxmlUtility.ParseType(value);
                if (m_EnumType == null)
                {
                    this.value = null;
                    textElement.text = string.Empty;
                }
            }
        }

        internal string valueAsString
        {
            get => value?.ToString();
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            set
            {
                if (type != null)
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        if (Enum.TryParse(type, value, false, out var result) && result is Enum enumValue)
                        {
                            Init(enumValue, includeObsoleteValues);
                            return;
                        }
                        else
                        {
                            // If we didn't have a valid value, try to set the type.
                            PopulateDataFromType(type);
                            this.value = null;
                        }
                    }
                    else
                    {
                        var enumValue = (Enum)Enum.ToObject(type, 0);
                        Init(enumValue, includeObsoleteValues);
                    }
                }
                else
                {
                    this.value = null;
                }
            }
        }

        /// <summary>
        /// Constructs an EnumFlagsField with a default value, and initializes its underlying type.
        /// </summary>
        /// <param name="defaultValue">Initial value. This also detects the Enum type.</param>
        public EnumFlagsField(Enum defaultValue)
            : this(null, defaultValue, false) {}

        /// <summary>
        /// Constructs an EnumFlagsField with a default value, and initializes its underlying type.
        /// </summary>
        /// <param name="defaultValue">Initial value. This also detects the Enum type.</param>
        public EnumFlagsField(Enum defaultValue, bool includeObsoleteValues)
            : this(null, defaultValue, includeObsoleteValues) {}

        /// <summary>
        /// Constructs an EnumFlagsField with a default value, and initializes its underlying type.
        /// </summary>
        /// <param name="defaultValue">Initial value. This also detects the Enum type.</param>
        public EnumFlagsField(string label, Enum defaultValue)
            : this(label, defaultValue, false)
        {
        }

        /// <summary>
        /// Constructs an EnumFlagsField with a default value, and initializes its underlying type.
        /// </summary>
        public EnumFlagsField()
            : this(null, null, false) {}


        /// <summary>
        /// Constructs an EnumFlagsField with a default value, and initializes its underlying type.
        /// </summary>
        /// <param name="defaultValue">Initial value. This also detects the Enum type.</param>
        public EnumFlagsField(string label, Enum defaultValue, bool includeObsoleteValues)
            : this(label)
        {
            if (defaultValue != null)
            {
                Init(defaultValue, includeObsoleteValues);
            }
        }

        /// <summary>
        /// Initializes the EnumFlagsField with a default value, and initializes its underlying type.
        /// </summary>
        /// <param name="defaultValue">The typed enum value.</param>
        /// <param name="includeObsoleteValues">Set to true to display obsolete values as choices.</param>
        public void Init(Enum defaultValue, bool includeObsoleteValues = false)
        {
            if (defaultValue == null)
            {
                throw new ArgumentNullException(nameof(defaultValue));
            }

            m_IncludeObsoleteValues = includeObsoleteValues;
            PopulateDataFromType(defaultValue.GetType());

            if (!m_EnumData.flags)
                Debug.LogWarning("EnumMaskField is not bound to enum type with the [Flags] attribute");

            choicesMasks = new List<int>(m_EnumData.flagValues);
            choices = new List<string>(m_EnumData.displayNames);

            SetValueWithoutNotify(defaultValue);
        }

        /// <summary>
        /// Constructs an EnumFlagsField with a default value, and initializes its underlying type.
        /// </summary>
        public EnumFlagsField(string label)
            : base(label)
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
        }

        internal override Enum MaskToValue(int newMask)
        {
            if (m_EnumType == null)
                return null;

            return EnumDataUtility.IntToEnumFlags(m_EnumType, newMask);
        }

        internal override int ValueToMask(Enum value)
        {
            if (m_EnumType == null)
                return 0;

            return EnumDataUtility.EnumFlagsToInt(m_EnumData, value);
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void PopulateDataFromType(Type enumType)
        {
            m_EnumType = enumType;
            m_EnumData = EnumDataUtility.GetCachedEnumData(m_EnumType, !includeObsoleteValues);
        }

        internal override string GetNothingName()
        {
            if (m_EnumData.flagValues is not { Length: > 0 })
                return base.GetNothingName();

            for (var i = 0; i < m_EnumData.flagValues.Length; i++)
            {
                if (m_EnumData.flagValues[i] == 0)
                {
                    return m_EnumData.displayNames[i];
                }
            }

            return base.GetNothingName();
        }

        internal override string GetEverythingName()
        {
            if (m_EnumData.flagValues is not { Length: > 0 })
                return base.GetEverythingName();

            for (var i = 0; i < m_EnumData.flagValues.Length; i++)
            {
                if (m_EnumData.flagValues[i] == ~0)
                {
                    return m_EnumData.displayNames[i];
                }
            }

            return base.GetEverythingName();
        }
    }
}
