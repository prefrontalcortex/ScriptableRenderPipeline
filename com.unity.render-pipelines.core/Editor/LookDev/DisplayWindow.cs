using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.LookDev
{
    /// <summary>Interface that must implement the viewer to communicate with the compositor and data management</summary>
    public interface IViewDisplayer
    {
        Rect GetRect(ViewCompositionIndex index);
        void SetTexture(ViewCompositionIndex index, Texture texture);

        void Repaint();

        event Action<Layout, SidePanel> OnLayoutChanged;

        event Action OnRenderDocAcquisitionTriggered;

        event Action<IMouseEvent> OnMouseEventInView;

        event Action<GameObject, ViewCompositionIndex, Vector2> OnChangingObjectInView;
        event Action<UnityEngine.Object, ViewCompositionIndex, Vector2> OnChangingEnvironmentInView;

        event Action OnClosed;

        event Action OnUpdateRequested;
    }
    
    partial class DisplayWindow : EditorWindow, IViewDisplayer
    {
        static partial class Style
        {
            internal const string k_IconFolder = @"Packages/com.unity.render-pipelines.core/Editor/LookDev/Icons/";
            internal const string k_uss = @"Packages/com.unity.render-pipelines.core/Editor/LookDev/DisplayWindow.uss";
            internal const string k_uss_personal_overload = @"Packages/com.unity.render-pipelines.core/Editor/LookDev/DisplayWindow-PersonalSkin.uss";

            public static readonly GUIContent WindowTitleAndIcon = EditorGUIUtility.TrTextContentWithIcon("Look Dev", CoreEditorUtils.LoadIcon(k_IconFolder, "LookDevMainIcon", forceLowRes: true));
        }
        
        // /!\ WARNING:
        //The following const are used in the uss.
        //If you change them, update the uss file too.
        internal const string k_MainContainerName = "mainContainer";
        internal const string k_ViewContainerName = "viewContainer";
        internal const string k_FirstViewName = "firstView";
        internal const string k_SecondViewName = "secondView";
        internal const string k_ToolbarName = "toolbar";
        internal const string k_ToolbarRadioName = "toolbarRadio";
        internal const string k_TabsRadioName = "tabsRadio";
        internal const string k_SideToolbarName = "sideToolbar";
        internal const string k_SharedContainerClass = "container";
        internal const string k_FirstViewClass = "firstView";
        internal const string k_SecondViewsClass = "secondView";
        internal const string k_VerticalViewsClass = "verticalSplit";
        internal const string k_DebugContainerName = "debugContainer";
        internal const string k_ShowDebugPanelClass = "showDebugPanel";

        VisualElement m_MainContainer;
        VisualElement m_ViewContainer;
        VisualElement m_DebugContainer;
        PopupField<string> m_DebugView;
        Label m_NoEnvironmentList;
        Label m_NoObject1;
        Label m_NoEnvironment1;
        Label m_NoObject2;
        Label m_NoEnvironment2;

        Image[] m_Views = new Image[2];

        Layout layout
        {
            get => LookDev.currentContext.layout.viewLayout;
            set
            {
                if (LookDev.currentContext.layout.viewLayout != value)
                {
                    OnLayoutChangedInternal?.Invoke(value, sidePanel);
                    ApplyLayout(value);
                }
            }
        }

        SidePanel sidePanel
        {
            get => LookDev.currentContext.layout.showedSidePanel;
            set
            {
                if (LookDev.currentContext.layout.showedSidePanel != value)
                {
                    OnLayoutChangedInternal?.Invoke(layout, value);
                    ApplySidePanelChange(value);
                }
            }
        }

        event Action<Layout, SidePanel> OnLayoutChangedInternal;
        event Action<Layout, SidePanel> IViewDisplayer.OnLayoutChanged
        {
            add => OnLayoutChangedInternal += value;
            remove => OnLayoutChangedInternal -= value;
        }

        event Action OnRenderDocAcquisitionTriggeredInternal;
        event Action IViewDisplayer.OnRenderDocAcquisitionTriggered
        {
            add => OnRenderDocAcquisitionTriggeredInternal += value;
            remove => OnRenderDocAcquisitionTriggeredInternal -= value;
        }

        event Action<IMouseEvent> OnMouseEventInViewPortInternal;
        event Action<IMouseEvent> IViewDisplayer.OnMouseEventInView
        {
            add => OnMouseEventInViewPortInternal += value;
            remove => OnMouseEventInViewPortInternal -= value;
        }

        event Action<GameObject, ViewCompositionIndex, Vector2> OnChangingObjectInViewInternal;
        event Action<GameObject, ViewCompositionIndex, Vector2> IViewDisplayer.OnChangingObjectInView
        {
            add => OnChangingObjectInViewInternal += value;
            remove => OnChangingObjectInViewInternal -= value;
        }

        //event Action<Material, ViewCompositionIndex, Vector2> OnChangingMaterialInViewInternal;
        //event Action<Material, ViewCompositionIndex, Vector2> IViewDisplayer.OnChangingMaterialInView
        //{
        //    add => OnChangingMaterialInViewInternal += value;
        //    remove => OnChangingMaterialInViewInternal -= value;
        //}

        event Action<UnityEngine.Object, ViewCompositionIndex, Vector2> OnChangingEnvironmentInViewInternal;
        event Action<UnityEngine.Object, ViewCompositionIndex, Vector2> IViewDisplayer.OnChangingEnvironmentInView
        {
            add => OnChangingEnvironmentInViewInternal += value;
            remove => OnChangingEnvironmentInViewInternal -= value;
        }

        event Action OnClosedInternal;
        event Action IViewDisplayer.OnClosed
        {
            add => OnClosedInternal += value;
            remove => OnClosedInternal -= value;
        }

        event Action OnUpdateRequestedInternal;
        event Action IViewDisplayer.OnUpdateRequested
        {
            add => OnUpdateRequestedInternal += value;
            remove => OnUpdateRequestedInternal -= value;
        }

        void OnEnable()
        {
            //Call the open function to configure LookDev
            // in case the window where open when last editor session finished.
            // (Else it will open at start and has nothing to display).
            if (!LookDev.open)
                LookDev.Open();

            titleContent = Style.WindowTitleAndIcon;

            rootVisualElement.styleSheets.Add(
                AssetDatabase.LoadAssetAtPath<StyleSheet>(Style.k_uss));

            if (!EditorGUIUtility.isProSkin)
            {
                rootVisualElement.styleSheets.Add(
                    AssetDatabase.LoadAssetAtPath<StyleSheet>(Style.k_uss_personal_overload));
            }

            CreateToolbar();

            m_MainContainer = new VisualElement() { name = k_MainContainerName };
            m_MainContainer.AddToClassList(k_SharedContainerClass);
            rootVisualElement.Add(m_MainContainer);

            CreateViews();
            CreateEnvironment();
            CreateDebug();
            CreateDropAreas();

            ApplyLayout(layout);
            ApplySidePanelChange(sidePanel);
        }

        void OnDisable() => OnClosedInternal?.Invoke();

        void CreateToolbar()
        {
            // Layout swapper part
            var layoutRadio = new ToolbarRadio() { name = k_ToolbarRadioName };
            layoutRadio.AddRadios(new[] {
                CoreEditorUtils.LoadIcon(Style.k_IconFolder, "LookDev_Layout1", forceLowRes: true),
                CoreEditorUtils.LoadIcon(Style.k_IconFolder, "LookDev_Layout2", forceLowRes: true),
                CoreEditorUtils.LoadIcon(Style.k_IconFolder, "LookDev_LayoutVertical", forceLowRes: true),
                CoreEditorUtils.LoadIcon(Style.k_IconFolder, "LookDev_LayoutHorizontal", forceLowRes: true),
                CoreEditorUtils.LoadIcon(Style.k_IconFolder, "LookDev_LayoutCustom", forceLowRes: true)
                });
            layoutRadio.RegisterCallback((ChangeEvent<int> evt)
                => layout = (Layout)evt.newValue);
            layoutRadio.SetValueWithoutNotify((int)layout);

            var cameraMenu = new ToolbarMenu() { name = "cameraMenu" };
            cameraMenu.variant = ToolbarMenu.Variant.Popup;
            var cameraToggle = new ToolbarToggle() { name = "cameraButton" };
            cameraToggle.value = LookDev.currentContext.cameraSynced;

            //Note: when having Image on top of the Toggle nested in the Menu, RegisterValueChangedCallback is not called
            //cameraToggle.RegisterValueChangedCallback(evt => LookDev.currentContext.cameraSynced = evt.newValue);
            cameraToggle.RegisterCallback<MouseUpEvent>(evt =>
            {
                LookDev.currentContext.cameraSynced ^= true;
                cameraToggle.SetValueWithoutNotify(LookDev.currentContext.cameraSynced);
            });

            var cameraSeparator = new ToolbarToggle() { name = "cameraSeparator" };
            var texCamera1 = CoreEditorUtils.LoadIcon(Style.k_IconFolder, "LookDev_Camera1", forceLowRes: true);
            var texCamera2 = CoreEditorUtils.LoadIcon(Style.k_IconFolder, "LookDev_Camera2", forceLowRes: true);
            var texLink = CoreEditorUtils.LoadIcon(Style.k_IconFolder, "LookDev_Link", forceLowRes: true);
            var texRight = CoreEditorUtils.LoadIcon(Style.k_IconFolder, "LookDev_Right", forceLowRes: true);
            var texLeft = CoreEditorUtils.LoadIcon(Style.k_IconFolder, "LookDev_Left", forceLowRes: true);
            cameraToggle.Add(new Image() { image = texCamera1 });
            cameraToggle.Add(new Image() { image = texLink });
            cameraToggle.Add(new Image() { image = texCamera2 });
            cameraMenu.Add(cameraToggle);
            cameraMenu.Add(cameraSeparator);
            cameraMenu.menu.AppendAction("Align Camera 1 with Camera 2",
                (DropdownMenuAction a) => LookDev.currentContext.SynchronizeCameraStates(ViewIndex.Second),
                DropdownMenuAction.AlwaysEnabled);
            cameraMenu.menu.AppendAction("Align Camera 2 with Camera 1",
                (DropdownMenuAction a) => LookDev.currentContext.SynchronizeCameraStates(ViewIndex.First),
                DropdownMenuAction.AlwaysEnabled);
            cameraMenu.menu.AppendAction("Reset Cameras",
                (DropdownMenuAction a) =>
                {
                    LookDev.currentContext.GetViewContent(ViewIndex.First).ResetCameraState();
                    LookDev.currentContext.GetViewContent(ViewIndex.Second).ResetCameraState();
                },
                DropdownMenuAction.AlwaysEnabled);

            // Side part
            var sideRadio = new ToolbarRadio(canDeselectAll: true) { name = k_TabsRadioName };
            sideRadio.AddRadios(new[] {
                "Environment",
                "Debug"
            });
            sideRadio.RegisterCallback((ChangeEvent<int> evt) =>
            {
                sidePanel = (SidePanel)evt.newValue;
                if (sidePanel == SidePanel.Debug)
                    RefreshDebugViews();
            });
            sideRadio.SetValueWithoutNotify((int)sidePanel);

            var sideToolbar = new Toolbar() { name = k_SideToolbarName };
            sideToolbar.Add(sideRadio);

            // Aggregate parts
            var toolbar = new Toolbar() { name = k_ToolbarName };
            toolbar.Add(layoutRadio);
            toolbar.Add(new ToolbarSpacer());
            toolbar.Add(cameraMenu);

            toolbar.Add(new ToolbarSpacer() { flex = true });
            if (UnityEditorInternal.RenderDoc.IsInstalled() && UnityEditorInternal.RenderDoc.IsLoaded())
            {
                var renderDocButton = new ToolbarButton(() => OnRenderDocAcquisitionTriggeredInternal?.Invoke())
                {
                    name = "renderdoc-content"
                };
                renderDocButton.Add(new Image()
                {
                    image = CoreEditorUtils.LoadIcon(Style.k_IconFolder, "renderdoc", forceLowRes: true)
                });
                renderDocButton.Add(new Label() { text = " Content" });
                toolbar.Add(renderDocButton);
            }
            toolbar.Add(sideToolbar);
            rootVisualElement.Add(toolbar);
        }

        void CreateViews()
        {
            if (m_MainContainer == null || m_MainContainer.Equals(null))
                throw new System.MemberAccessException("m_MainContainer should be assigned prior CreateViews()");

            m_ViewContainer = new VisualElement() { name = k_ViewContainerName };
            m_ViewContainer.AddToClassList(LookDev.currentContext.layout.isMultiView ? k_SecondViewsClass : k_FirstViewClass);
            m_ViewContainer.AddToClassList(k_SharedContainerClass);
            m_MainContainer.Add(m_ViewContainer);
            m_ViewContainer.RegisterCallback<MouseDownEvent>(evt => OnMouseEventInViewPortInternal?.Invoke(evt));
            m_ViewContainer.RegisterCallback<MouseUpEvent>(evt => OnMouseEventInViewPortInternal?.Invoke(evt));
            m_ViewContainer.RegisterCallback<MouseMoveEvent>(evt => OnMouseEventInViewPortInternal?.Invoke(evt));

            m_Views[(int)ViewIndex.First] = new Image() { name = k_FirstViewName, image = Texture2D.blackTexture };
            m_ViewContainer.Add(m_Views[(int)ViewIndex.First]);
            m_Views[(int)ViewIndex.Second] = new Image() { name = k_SecondViewName, image = Texture2D.blackTexture };
            m_ViewContainer.Add(m_Views[(int)ViewIndex.Second]);

            var firstOrCompositeManipulator = new SwitchableCameraController(
                LookDev.currentContext.GetViewContent(ViewIndex.First).camera,
                LookDev.currentContext.GetViewContent(ViewIndex.Second).camera,
                this,
                index =>
                {
                    LookDev.currentContext.SetFocusedCamera(index);
                    var environment = LookDev.currentContext.GetViewContent(index).environment;
                    if (sidePanel == SidePanel.Environment && environment != null)
                        m_EnvironmentList.selectedIndex = LookDev.currentContext.environmentLibrary.IndexOf(environment);
                });
            var secondManipulator = new CameraController(
                LookDev.currentContext.GetViewContent(ViewIndex.Second).camera,
                this,
                () =>
                {
                    LookDev.currentContext.SetFocusedCamera(ViewIndex.Second);
                    var environment = LookDev.currentContext.GetViewContent(ViewIndex.Second).environment;
                    if (sidePanel == SidePanel.Environment && environment != null)
                        m_EnvironmentList.selectedIndex = LookDev.currentContext.environmentLibrary.IndexOf(environment);
                });
            var gizmoManipulator = new ComparisonGizmoController(LookDev.currentContext.layout.gizmoState, firstOrCompositeManipulator);
            m_Views[(int)ViewIndex.First].AddManipulator(gizmoManipulator); //must take event first to switch the firstOrCompositeManipulator
            m_Views[(int)ViewIndex.First].AddManipulator(firstOrCompositeManipulator);
            m_Views[(int)ViewIndex.Second].AddManipulator(secondManipulator);

            m_NoObject1 = new Label("Drag'n'drop object here");
            m_NoObject1.style.flexGrow = 1;
            m_NoObject1.style.unityTextAlign = TextAnchor.MiddleCenter;
            m_NoObject2 = new Label("Drag'n'drop object here");
            m_NoObject2.style.flexGrow = 1;
            m_NoObject2.style.unityTextAlign = TextAnchor.MiddleCenter;
            m_NoEnvironment1 = new Label("Drag'n'drop environment from side panel here");
            m_NoEnvironment1.style.flexGrow = 1;
            m_NoEnvironment1.style.unityTextAlign = TextAnchor.MiddleCenter;
            m_NoEnvironment2 = new Label("Drag'n'drop environment from side panel here");
            m_NoEnvironment2.style.flexGrow = 1;
            m_NoEnvironment2.style.unityTextAlign = TextAnchor.MiddleCenter;
            m_Views[(int)ViewIndex.First].Add(m_NoObject1);
            m_Views[(int)ViewIndex.First].Add(m_NoEnvironment1);
            m_Views[(int)ViewIndex.Second].Add(m_NoObject2);
            m_Views[(int)ViewIndex.Second].Add(m_NoEnvironment2);
        }

        void CreateDropAreas()
        {
            // GameObject or Prefab in view
            new DropArea(new[] { typeof(GameObject) }, m_Views[(int)ViewIndex.First], (obj, localPos) =>
            {
                if (layout == Layout.CustomSplit)
                    OnChangingObjectInViewInternal?.Invoke(obj as GameObject, ViewCompositionIndex.Composite, localPos);
                else
                    OnChangingObjectInViewInternal?.Invoke(obj as GameObject, ViewCompositionIndex.First, localPos);
                m_NoObject1.style.visibility = obj == null || obj.Equals(null) ? Visibility.Visible : Visibility.Hidden;
            });
            new DropArea(new[] { typeof(GameObject) }, m_Views[(int)ViewIndex.Second], (obj, localPos) =>
            {
                OnChangingObjectInViewInternal?.Invoke(obj as GameObject, ViewCompositionIndex.Second, localPos);
                m_NoObject2.style.visibility = obj == null || obj.Equals(null) ? Visibility.Visible : Visibility.Hidden;
            });

            // Material in view
            //new DropArea(new[] { typeof(GameObject) }, m_Views[(int)ViewIndex.First], (obj, localPos) =>
            //{
            //    if (layout == Layout.CustomSplit || layout == Layout.CustomCircular)
            //        OnChangingMaterialInViewInternal?.Invoke(obj as Material, ViewCompositionIndex.Composite, localPos);
            //    else
            //        OnChangingMaterialInViewInternal?.Invoke(obj as Material, ViewCompositionIndex.First, localPos);
            //});
            //new DropArea(new[] { typeof(Material) }, m_Views[(int)ViewIndex.Second], (obj, localPos)
            //    => OnChangingMaterialInViewInternal?.Invoke(obj as Material, ViewCompositionIndex.Second, localPos));

            // Environment in view
            new DropArea(new[] { typeof(Environment), typeof(Cubemap) }, m_Views[(int)ViewIndex.First], (obj, localPos) =>
            {
                if (layout == Layout.CustomSplit)
                    OnChangingEnvironmentInViewInternal?.Invoke(obj, ViewCompositionIndex.Composite, localPos);
                else
                    OnChangingEnvironmentInViewInternal?.Invoke(obj, ViewCompositionIndex.First, localPos);
                m_NoEnvironment1.style.visibility = obj == null || obj.Equals(null) ? Visibility.Visible : Visibility.Hidden;
            });
            new DropArea(new[] { typeof(Environment), typeof(Cubemap) }, m_Views[(int)ViewIndex.Second], (obj, localPos) =>
            {
                OnChangingEnvironmentInViewInternal?.Invoke(obj, ViewCompositionIndex.Second, localPos);
                m_NoEnvironment2.style.visibility = obj == null || obj.Equals(null) ? Visibility.Visible : Visibility.Hidden;
            });

            // Environment in library
            //new DropArea(new[] { typeof(Environment), typeof(Cubemap) }, m_EnvironmentContainer, (obj, localPos) =>
            //{
            //    //[TODO: check if this come from outside of library]
            //    OnAddingEnvironmentInternal?.Invoke(obj);
            //});
            new DropArea(new[] { typeof(EnvironmentLibrary) }, m_EnvironmentContainer, (obj, localPos) =>
            {
                OnChangingEnvironmentLibraryInternal?.Invoke(obj as EnvironmentLibrary);
                RefreshLibraryDisplay();
            });
        }

        Rect IViewDisplayer.GetRect(ViewCompositionIndex index)
        {
            switch (index)
            {
                case ViewCompositionIndex.First:
                case ViewCompositionIndex.Composite:    //display composition on first rect
                    return m_Views[(int)ViewIndex.First].contentRect;
                case ViewCompositionIndex.Second:
                    return m_Views[(int)ViewIndex.Second].contentRect;
                default:
                    throw new ArgumentException("Unknown ViewCompositionIndex: " + index);
            }
        }
        
        Vector2 m_LastFirstViewSize = new Vector2();
        Vector2 m_LastSecondViewSize = new Vector2();
        void IViewDisplayer.SetTexture(ViewCompositionIndex index, Texture texture)
        {
            bool updated = false;
            switch (index)
            {
                case ViewCompositionIndex.First:
                case ViewCompositionIndex.Composite:    //display composition on first rect
                    if (updated |= m_Views[(int)ViewIndex.First].image != texture)
                        m_Views[(int)ViewIndex.First].image = texture;
                    else if (updated |= (m_LastFirstViewSize.x != texture.width
                                      || m_LastFirstViewSize.y != texture.height))
                    {
                        m_Views[(int)ViewIndex.First].image = null; //force refresh else it will appear zoomed
                        m_Views[(int)ViewIndex.First].image = texture;
                    }
                    if (updated)
                    {
                        m_LastFirstViewSize.x = texture?.width ?? 0;
                        m_LastFirstViewSize.y = texture?.height ?? 0;
                    }
                    break;
                case ViewCompositionIndex.Second:
                    if (m_Views[(int)ViewIndex.Second].image != texture)
                        m_Views[(int)ViewIndex.Second].image = texture;
                    else if (updated |= (m_LastSecondViewSize.x != texture.width
                                      || m_LastSecondViewSize.y != texture.height))
                    {
                        m_Views[(int)ViewIndex.Second].image = null; //force refresh else it will appear zoomed
                        m_Views[(int)ViewIndex.Second].image = texture;
                    }
                    if (updated)
                    {
                        m_LastSecondViewSize.x = texture?.width ?? 0;
                        m_LastSecondViewSize.y = texture?.height ?? 0;
                    }
                    break;
                default:
                    throw new ArgumentException("Unknown ViewCompositionIndex: " + index);
            }
        }
        
        void IViewDisplayer.Repaint() => Repaint();

        void ApplyLayout(Layout value)
        {
            m_NoObject1.style.visibility = LookDev.currentContext.GetViewContent(ViewIndex.First).hasViewedObject ? Visibility.Hidden : Visibility.Visible;
            m_NoObject2.style.visibility = LookDev.currentContext.GetViewContent(ViewIndex.Second).hasViewedObject ? Visibility.Hidden : Visibility.Visible;
            m_NoEnvironment1.style.visibility = LookDev.currentContext.GetViewContent(ViewIndex.First).hasEnvironment ? Visibility.Hidden : Visibility.Visible;
            m_NoEnvironment2.style.visibility = LookDev.currentContext.GetViewContent(ViewIndex.Second).hasEnvironment ? Visibility.Hidden : Visibility.Visible;

            switch (value)
            {
                case Layout.HorizontalSplit:
                case Layout.VerticalSplit:
                    if (!m_ViewContainer.ClassListContains(k_FirstViewClass))
                        m_ViewContainer.AddToClassList(k_FirstViewClass);
                    if (!m_ViewContainer.ClassListContains(k_SecondViewsClass))
                        m_ViewContainer.AddToClassList(k_SecondViewsClass);
                    if (value == Layout.VerticalSplit)
                    {
                        m_ViewContainer.AddToClassList(k_VerticalViewsClass);
                        if (!m_ViewContainer.ClassListContains(k_VerticalViewsClass))
                            m_ViewContainer.AddToClassList(k_FirstViewClass);
                    }
                    for (int i = 0; i < 2; ++i)
                        m_Views[i].style.display = DisplayStyle.Flex;
                    break;
                case Layout.FullFirstView:
                case Layout.CustomSplit:       //display composition on first rect
                    if (!m_ViewContainer.ClassListContains(k_FirstViewClass))
                        m_ViewContainer.AddToClassList(k_FirstViewClass);
                    if (m_ViewContainer.ClassListContains(k_SecondViewsClass))
                        m_ViewContainer.RemoveFromClassList(k_SecondViewsClass);
                    m_Views[0].style.display = DisplayStyle.Flex;
                    m_Views[1].style.display = DisplayStyle.None;
                    break;
                case Layout.FullSecondView:
                    if (m_ViewContainer.ClassListContains(k_FirstViewClass))
                        m_ViewContainer.RemoveFromClassList(k_FirstViewClass);
                    if (!m_ViewContainer.ClassListContains(k_SecondViewsClass))
                        m_ViewContainer.AddToClassList(k_SecondViewsClass);
                    m_Views[0].style.display = DisplayStyle.None;
                    m_Views[1].style.display = DisplayStyle.Flex;
                    break;
                default:
                    throw new ArgumentException("Unknown Layout");
            }

            //Add flex direction here
            if (value == Layout.VerticalSplit)
                m_ViewContainer.AddToClassList(k_VerticalViewsClass);
            else if (m_ViewContainer.ClassListContains(k_VerticalViewsClass))
                m_ViewContainer.RemoveFromClassList(k_VerticalViewsClass);
        }

        void ApplySidePanelChange(SidePanel sidePanel)
        {
            switch (sidePanel)
            {
                case SidePanel.None:
                    if (m_MainContainer.ClassListContains(k_ShowEnvironmentPanelClass))
                        m_MainContainer.RemoveFromClassList(k_ShowEnvironmentPanelClass);
                    if (m_MainContainer.ClassListContains(k_ShowDebugPanelClass))
                        m_MainContainer.RemoveFromClassList(k_ShowDebugPanelClass);
                    m_EnvironmentContainer.Q<EnvironmentElement>().style.visibility = Visibility.Hidden;
                    m_EnvironmentContainer.Q(className: "unity-base-slider--vertical").Q("unity-dragger").style.display = DisplayStyle.None;
                    m_EnvironmentContainer.style.display = DisplayStyle.None;
                    break;
                case SidePanel.Environment:
                    if (!m_MainContainer.ClassListContains(k_ShowEnvironmentPanelClass))
                        m_MainContainer.AddToClassList(k_ShowEnvironmentPanelClass);
                    if (m_MainContainer.ClassListContains(k_ShowDebugPanelClass))
                        m_MainContainer.RemoveFromClassList(k_ShowDebugPanelClass);
                    if (m_EnvironmentList.selectedIndex != -1)
                        m_EnvironmentContainer.Q<EnvironmentElement>().style.visibility = Visibility.Visible;
                    m_EnvironmentContainer.Q(className: "unity-base-slider--vertical").Q("unity-dragger").style.display = DisplayStyle.Flex;
                    m_EnvironmentContainer.style.display = DisplayStyle.Flex;
                    break;
                case SidePanel.Debug:
                    if (m_MainContainer.ClassListContains(k_ShowEnvironmentPanelClass))
                        m_MainContainer.RemoveFromClassList(k_ShowEnvironmentPanelClass);
                    if (!m_MainContainer.ClassListContains(k_ShowDebugPanelClass))
                        m_MainContainer.AddToClassList(k_ShowDebugPanelClass);
                    m_EnvironmentContainer.Q<EnvironmentElement>().style.visibility = Visibility.Hidden;
                    m_EnvironmentContainer.Q(className: "unity-base-slider--vertical").Q("unity-dragger").style.display = DisplayStyle.None;
                    m_EnvironmentContainer.style.display = DisplayStyle.None;
                    break;
                default:
                    throw new ArgumentException("Unknown SidePanel");
            }
        }

        void OnGUI() => OnUpdateRequestedInternal?.Invoke();
    }
}
