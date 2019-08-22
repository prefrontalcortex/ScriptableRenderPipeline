using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.LookDev
{   
    partial class DisplayWindow
    {
        bool cameraSynced
            => LookDev.currentContext.cameraSynced;

        ViewContext lastFocusedViewContext
            => LookDev.currentContext.GetViewContent(LookDev.currentContext.layout.lastFocusedView);

        void ForEachViewsContext(Action<ViewContext> action)
        {
            action?.Invoke(LookDev.currentContext.GetViewContent(ViewIndex.First));
            action?.Invoke(LookDev.currentContext.GetViewContent(ViewIndex.Second));
        }

        void CreateDebug()
        {
            if (m_MainContainer == null || m_MainContainer.Equals(null))
                throw new System.MemberAccessException("m_MainContainer should be assigned prior CreateEnvironment()");

            m_DebugContainer = new VisualElement() { name = k_DebugContainerName };
            m_MainContainer.Add(m_DebugContainer);
            if (sidePanel == SidePanel.Debug)
                m_MainContainer.AddToClassList(k_ShowDebugPanelClass);
            
            Toggle shadow = new Toggle("Display Shadows");
            shadow.value = lastFocusedViewContext.debug.shadow;
            shadow.RegisterValueChangedCallback(evt =>
            {
                if (cameraSynced)
                    ForEachViewsContext(view => view.debug.shadow = evt.newValue);
                else 
                lastFocusedViewContext.debug.shadow = evt.newValue;
            });
            m_DebugContainer.Add(shadow);
            
            //[TODO: finish]
            //Toggle greyBalls = new Toggle("Grey balls");
            //greyBalls.SetValueWithoutNotify(LookDev.currentContext.GetViewContent(LookDev.currentContext.layout.lastFocusedView).debug.greyBalls);
            //greyBalls.RegisterValueChangedCallback(evt =>
            //{
            //    LookDev.currentContext.GetViewContent(LookDev.currentContext.layout.lastFocusedView).debug.greyBalls = evt.newValue;
            //});
            //m_DebugContainer.Add(greyBalls);

            //[TODO: debug why list sometimes empty on resource reloading]
            //[TODO: display only per view]

            RefreshDebugViews();
        }

        void RefreshDebugViews()
        {
            if (m_DebugView != null && m_DebugContainer.Contains(m_DebugView))
                m_DebugContainer.Remove(m_DebugView);

            List<string> list = new List<string>(LookDev.dataProvider?.supportedDebugModes ?? Enumerable.Empty<string>());
            list.Insert(0, "None");
            m_DebugView = new PopupField<string>("Debug view mode", list, 0);
            m_DebugView.RegisterValueChangedCallback(evt
                => LookDev.dataProvider.UpdateDebugMode(list.IndexOf(evt.newValue) - 1));
            m_DebugContainer.Add(m_DebugView);
        }
        
    }
}
