// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    internal class ListViewReorderableDragAndDropController : BaseReorderableDragAndDropController
    {
        protected readonly BaseListView m_ListView;

        public ListViewReorderableDragAndDropController(BaseListView view)
            : base(view)
        {
            m_ListView = view;
        }

        public override DragVisualMode HandleDragAndDrop(IListDragAndDropArgs args)
        {
            if (args.dragAndDropPosition == DragAndDropPosition.OverItem)
                return DragVisualMode.Rejected;

            return args.dragAndDropData.source == m_ListView ?
                DragVisualMode.Move :
                DragVisualMode.Rejected;
        }

        public override void OnDrop(IListDragAndDropArgs args)
        {
            if (!m_ListView.reorderable)
                return;

            var insertIndex = args.insertAtIndex;

            var insertIndexShift = 0;
            var srcIndexShift = 0;
            for (var i = m_SortedSelectedIds.Count - 1; i >= 0; --i)
            {
                var id = m_SortedSelectedIds[i];
                var index = m_View.viewController.GetIndexForId(id);

                if (index < 0)
                    continue;

                var newIndex = insertIndex - insertIndexShift;

                if (index >= insertIndex)
                {
                    index += srcIndexShift;
                    srcIndexShift++;
                }
                else if (index < newIndex)
                {
                    insertIndexShift++;
                    newIndex--;
                }

                m_ListView.viewController.Move(index, newIndex);
            }

            if (m_ListView.selectionType != SelectionType.None)
            {
                var newSelection = new List<int>();

                for (var i = 0; i < m_SortedSelectedIds.Count; ++i)
                {
                    newSelection.Add(insertIndex - insertIndexShift + i);
                }

                m_ListView.SetSelectionWithoutNotify(newSelection);
            }
            else
            {
                m_ListView.ClearSelection();
            }
        }
    }
}
