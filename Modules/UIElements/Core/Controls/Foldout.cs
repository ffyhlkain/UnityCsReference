// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using Unity.Properties;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// A Foldout control is a collapsible section of a user interface. When toggled, it expands or collapses, which hides or reveals the elements it contains.
    /// </summary>
    /// <remarks>
    /// A Foldout consists of a <see cref="Toggle"/> sub-element and an empty <see cref="VisualElement"/>. The empty VisualElement
    /// is a container for the elements to show/hide when you expand/collapse the foldout. Foldout element's Toggle sub-element uses
    /// an arrow sprite instead of the <see cref="Toggle"/> control's usual checkbox. The arrow points right when the toggle is
    /// collapsed and down when it is expanded.
    /// </remarks>
    public class Foldout : BindableElement, INotifyValueChanged<bool>
    {
        internal static readonly BindingId textProperty = nameof(text);
        internal static readonly BindingId toggleOnLabelClickProperty = nameof(toggleOnLabelClick);
        internal static readonly BindingId valueProperty = nameof(value);

        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : BindableElement.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[]
                {
                    new (nameof(text), "text"),
                    new (nameof(toggleOnLabelClick), "toggle-on-label-click"),
                    new (nameof(value), "value"),
                });
            }

            #pragma warning disable 649
            [SerializeField, MultilineTextField] string text;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags text_UxmlAttributeFlags;
            [SerializeField] bool toggleOnLabelClick;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags toggleOnLabelClick_UxmlAttributeFlags;
            [SerializeField] bool value;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags value_UxmlAttributeFlags;
            #pragma warning restore 649

            public override object CreateInstance() => new Foldout();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                var e = (Foldout)obj;
                if (ShouldWriteAttributeValue(text_UxmlAttributeFlags))
                    e.text = text;
                if (ShouldWriteAttributeValue(toggleOnLabelClick_UxmlAttributeFlags))
                    e.toggleOnLabelClick = toggleOnLabelClick;
                if (ShouldWriteAttributeValue(value_UxmlAttributeFlags))
                    e.SetValueWithoutNotify(value);
            }
        }

        /// <summary>
        /// Instantiates a <see cref="Foldout"/> using the data from a UXML file.
        /// </summary>
        /// <remarks>
        /// This class is added to every <see cref="VisualElement"/> created from UXML.
        /// </remarks>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        public new class UxmlFactory : UxmlFactory<Foldout, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="Foldout"/>.
        /// </summary>
        /// <remarks>
        /// This class defines the Foldout element properties that you can use in a UXML asset.
        /// </remarks>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        public new class UxmlTraits : BindableElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_Text = new UxmlStringAttributeDescription { name = "text" };
            UxmlBoolAttributeDescription m_Value = new UxmlBoolAttributeDescription { name = "value", defaultValue = true };

            /// <summary>
            /// Initializes <see cref="Foldout"/> properties using values from the attribute bag.
            /// </summary>
            /// <param name="ve">The object to initialize.</param>
            /// <param name="bag">The attribute bag.</param>
            /// <param name="cc">The creation context; unused.</param>
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                Foldout f = ve as Foldout;
                if (f != null)
                {
                    f.text = m_Text.GetValueFromBag(bag, cc);
                    f.SetValueWithoutNotify(m_Value.GetValueFromBag(bag, cc));
                }
            }
        }

        readonly Toggle m_Toggle = new Toggle();

        internal Toggle toggle
        {
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            get => m_Toggle;
        }

        VisualElement m_Container;

        /// <summary>
        /// This element contains the elements that are shown or hidden when you toggle the <see cref="Foldout"/>.
        /// </summary>
        public override VisualElement contentContainer => m_Container;

        public override bool focusable
        {
            get => base.focusable;
            set
            {
                base.focusable = value;
                m_Toggle.focusable = value;
            }
        }

        /// <summary>
        /// Whether to toggle the foldout state when the user clicks the label.
        /// </summary>
        [CreateProperty]
        public bool toggleOnLabelClick
        {
            get => m_Toggle.toggleOnTextClick;
            set
            {
                if (m_Toggle.toggleOnTextClick == value)
                    return;

                m_Toggle.toggleOnTextClick = value;
                NotifyPropertyChanged(toggleOnLabelClickProperty);
            }
        }

        /// <summary>
        /// The label text for the toggle.
        /// </summary>
        [CreateProperty]
        public string text
        {
            get => m_Toggle.text;
            set
            {
                var previous = text;
                m_Toggle.text = value;
                m_Toggle.visualInput.Q(className: Toggle.textUssClassName)?.AddToClassList(textUssClassName);
                if (string.CompareOrdinal(previous, text) != 0)
                    NotifyPropertyChanged(textProperty);
            }
        }

        [SerializeField, DontCreateProperty]
        private bool m_Value;

        /// <summary>
        /// This is the state of the Foldout's toggle. It is true if the <see cref="Foldout"/> is open and its contents are
        /// visible, and false if the Foldout is closed, and its contents are hidden.
        /// </summary>
        [CreateProperty]
        public bool value
        {
            get => m_Value;
            set
            {
                if (m_Value == value)
                    return;

                using (ChangeEvent<bool> evt = ChangeEvent<bool>.GetPooled(m_Value, value))
                {
                    evt.elementTarget = this;
                    SetValueWithoutNotify(value);
                    SendEvent(evt);
                    SaveViewData();
                    NotifyPropertyChanged(valueProperty);
                }
            }
        }

        /// <summary>
        /// Sets the value of the Foldout's Toggle sub-element, but does not notify the rest of the hierarchy of the change.
        /// </summary>
        /// <remarks>
        /// This is useful when you want to change the Foldout's Toggle value without triggering events. For example, let's say you
        /// set up a Foldout to trigger an animation, but you only want to trigger the animation when a user clicks the Foldout's Toggle,
        /// not when you change the Toggle's value via code (for example, inside another validation). You could use this method
        /// change the value "silently".
        /// </remarks>
        /// <param name="newValue">The new value of the foldout</param>
        public void SetValueWithoutNotify(bool newValue)
        {
            m_Value = newValue;
            m_Toggle.SetValueWithoutNotify(m_Value);
            contentContainer.style.display = newValue ? DisplayStyle.Flex : DisplayStyle.None;
            if (m_Value)
            {
                pseudoStates |= PseudoStates.Checked;
            }
            else
            {
                pseudoStates &= ~PseudoStates.Checked;
            }
        }

        /// <summary>
        /// The USS class name for Foldout elements.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to every instance of a <see cref="Foldout"/>. Any styling applied to
        /// this class affects every Foldout located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public static readonly string ussClassName = "unity-foldout";
        /// <summary>
        /// The USS class name of Toggle sub-elements in Foldout elements.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to the <see cref="Toggle"/> sub-element of every <see cref="Foldout"/>.
        /// Any styling applied to this class affects every Toggle sub-element located beside, or below the
        /// stylesheet in the visual tree.
        /// </remarks>
        public static readonly string toggleUssClassName = ussClassName + "__toggle";
        /// <summary>
        /// The USS class name for the content element in a Foldout.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to the <see cref="VisualElement"/> that contains the elements to be shown
        /// or hidden. Any styling applied to this class affects every foldout container located beside, or
        /// below the stylesheet in the visual tree.
        /// </remarks>
        public static readonly string contentUssClassName = ussClassName + "__content";
        /// <summary>
        /// The USS class name for the Label element in a Foldout.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to the <see cref="VisualElement"/> that contains the <see cref="Toggle"/> input elements.
        /// Any styling applied to this class affects every foldout container located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public static readonly string inputUssClassName = ussClassName + "__input";
        /// <summary>
        /// The USS class name for the Label element in a Foldout.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to the <see cref="VisualElement"/> that represents the checkmark of the
        /// <see cref="Toggle"/> sub-element of every <see cref="Foldout"/>. Any styling applied to this class affects
        /// every foldout container located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public static readonly string checkmarkUssClassName = ussClassName + "__checkmark";
        /// <summary>
        /// The USS class name for the Label element in a Foldout.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to the <see cref="Label"/> in the <see cref="Toggle"/> sub-element of every
        /// <see cref="Foldout"/>. Any styling applied to this class affects every foldout container located beside, or
        /// below the stylesheet in the visual tree.
        /// </remarks>
        public static readonly string textUssClassName = ussClassName + "__text";

        internal static readonly string toggleInspectorUssClassName = toggleUssClassName + "--inspector";
        internal static readonly string ussFoldoutDepthClassName = ussClassName + "--depth-";
        internal static readonly int ussFoldoutMaxDepth = 4;

        private KeyboardNavigationManipulator m_NavigationManipulator;

        internal override void OnViewDataReady()
        {
            base.OnViewDataReady();

            string key = GetFullHierarchicalViewDataKey();

            OverwriteFromViewData(this, key);
            SetValueWithoutNotify(m_Value);
        }

        private void Apply(KeyboardNavigationOperation op, EventBase sourceEvent)
        {
            if (Apply(op))
            {
                sourceEvent.StopPropagation();
                focusController.IgnoreEvent(sourceEvent);
            }
        }

        private bool Apply(KeyboardNavigationOperation op)
        {
            switch (op)
            {
                case KeyboardNavigationOperation.Previous:
                case KeyboardNavigationOperation.Next:
                case KeyboardNavigationOperation.SelectAll:
                case KeyboardNavigationOperation.Cancel:
                case KeyboardNavigationOperation.Submit:
                case KeyboardNavigationOperation.Begin:
                case KeyboardNavigationOperation.End:
                case KeyboardNavigationOperation.PageDown:
                case KeyboardNavigationOperation.PageUp:
                    break; // Allow focus to move outside the Foldout
                case KeyboardNavigationOperation.MoveRight:
                    SetValueWithoutNotify(true);
                    return true;
                case KeyboardNavigationOperation.MoveLeft:
                    SetValueWithoutNotify(false);
                    return true;
                default:
                    throw new ArgumentOutOfRangeException(nameof(op), op, null);
            }

            return false;
        }

        /// <summary>
        /// Constructs a Foldout element.
        /// </summary>
        public Foldout()
        {
            AddToClassList(ussClassName);
            delegatesFocus = true;
            focusable = true;

            // We disable focus from disabled children to avoid the Foldout being focused when one of its children is focused.
            // This is to keep the behaviour consistent with how it worked before the `focusable` property was synced with its Toggle. (UUM-69153)
            isEligibleToReceiveFocusFromDisabledChild = false;

            m_Container = new VisualElement()
            {
                name = "unity-content",
            };

            m_Toggle.RegisterValueChangedCallback((evt) =>
            {
                value = m_Toggle.value;
                evt.StopPropagation();
            });
            m_Toggle.AddToClassList(toggleUssClassName);
            m_Toggle.visualInput.AddToClassList(inputUssClassName);
            m_Toggle.visualInput.Q(className: Toggle.checkmarkUssClassName).AddToClassList(checkmarkUssClassName);
            m_Toggle.AddManipulator(m_NavigationManipulator = new KeyboardNavigationManipulator(Apply));
            hierarchy.Add(m_Toggle);

            m_Container.AddToClassList(contentUssClassName);
            hierarchy.Add(m_Container);

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            SetValueWithoutNotify(true);
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            // Remove from all the depth classes...
            for (int i = 0; i <= ussFoldoutMaxDepth; i++)
            {
                RemoveFromClassList(ussFoldoutDepthClassName + i);
            }
            RemoveFromClassList(ussFoldoutDepthClassName + "max");

            m_Toggle.AssignInspectorStyleIfNecessary(toggleInspectorUssClassName);

            // Figure out the real depth of this actual Foldout...
            var depth = this.GetFoldoutDepth();

            // Add the class name corresponding to that depth
            if (depth > ussFoldoutMaxDepth)
            {
                AddToClassList(ussFoldoutDepthClassName + "max");
            }
            else
            {
                AddToClassList(ussFoldoutDepthClassName + depth);
            }
        }
    }
}
