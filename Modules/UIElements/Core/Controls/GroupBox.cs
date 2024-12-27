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
    /// This is an enclosing container for a group of <see cref="IGroupBoxOption"/>. All group options within this
    /// container will interact together to allow a single selection, using the <see cref="DefaultGroupManager"/>.
    /// Default options are <see cref="RadioButton"/>, but users can provide other implementations.
    /// If no <see cref="IGroupBox"/> is found in the hierarchy, the default container will be the panel.
    /// </summary>
    public class GroupBox : BindableElement, IGroupBox
    {
        internal static readonly BindingId textProperty = nameof(text);

        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : BindableElement.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[]
                {
                    new (nameof(text), "text"),
                });
            }

            #pragma warning disable 649
            [SerializeField, MultilineTextField] string text;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags text_UxmlAttributeFlags;
            #pragma warning restore 649

            public override object CreateInstance() => new GroupBox();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                if (ShouldWriteAttributeValue(text_UxmlAttributeFlags))
                {
                    var e = (GroupBox)obj;
                    e.text = text;
                }
            }
        }

        /// <summary>
        /// Instantiates a <see cref="GroupBox"/> using data from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        public new class UxmlFactory : UxmlFactory<GroupBox, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="GroupBox"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        public new class UxmlTraits : BindableElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_Text = new UxmlStringAttributeDescription { name = "text" };

            /// <summary>
            /// Initializes <see cref="GroupBox"/> properties using values from the attribute bag.
            /// </summary>
            /// <param name="ve">The object to initialize.</param>
            /// <param name="bag">The attribute bag.</param>
            /// <param name="cc">The creation context; unused.</param>
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                ((GroupBox)ve).text = m_Text.GetValueFromBag(bag, cc);
            }
        }

        /// <summary>
        /// USS class name for GroupBox elements.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to every instance of the GroupBox element. Any styling applied to
        /// this class affects every GroupBox located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public static readonly string ussClassName = "unity-group-box";
        /// <summary>
        /// USS class name for Labels in GroupBox elements.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to the <see cref="Label"/> sub-element of the <see cref="GroupBox"/> if the GroupBox has a Label.
        /// </remarks>
        public static readonly string labelUssClassName = ussClassName + "__label";

        Label m_TitleLabel;

        // Needed by the UIBuilder for authoring in the viewport
        internal Label titleLabel
        {
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            get => m_TitleLabel;
        }

        /// <summary>
        /// The title text of the box.
        /// </summary>
        [CreateProperty]
        public string text
        {
            get => m_TitleLabel?.text;
            set
            {
                var previous = text;

                if (!string.IsNullOrEmpty(value))
                {
                    // Lazy allocation of label if needed...
                    if (m_TitleLabel == null)
                    {
                        m_TitleLabel = new Label(value);
                        m_TitleLabel.AddToClassList(labelUssClassName);
                        Insert(0, m_TitleLabel);
                    }

                    m_TitleLabel.text = value;
                }
                else if (m_TitleLabel != null)
                {
                    m_TitleLabel.RemoveFromHierarchy();
                    m_TitleLabel = null;
                }

                if (string.CompareOrdinal(previous, text) != 0)
                    NotifyPropertyChanged(textProperty);
            }
        }

        /// <summary>
        /// Creates a <see cref="GroupBox"/> with no label.
        /// </summary>
        public GroupBox()
            : this(null) {}

        /// <summary>
        /// Creates a <see cref="GroupBox"/> with a title.
        /// </summary>
        /// <param name="text">The title text.</param>
        public GroupBox(string text)
        {
            AddToClassList(ussClassName);

            this.text = text;
        }

        void IGroupBox.OnOptionAdded(IGroupBoxOption option) { /* Nothing to do here. */ }
        void IGroupBox.OnOptionRemoved(IGroupBoxOption option) { /* Nothing to do here. */ }
    }
}
