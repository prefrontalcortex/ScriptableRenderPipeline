﻿using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    class LabelControl : IShaderControl
    {
        public ShaderControlData controlData { get; set; }
        public ShaderValueData defaultValueData { get; }

        public ConcreteSlotValueType[] validPortTypes
        {
            get { return (ConcreteSlotValueType[])Enum.GetValues(typeof(ConcreteSlotValueType)); }
        }

        public int portControlWidth
        {
            get { return 42; }
        }

        public LabelControl()
        {
            this.controlData = new ShaderControlData()
            {
                labels = new string[] { "Label" }
            };
        }

        public LabelControl(string label, ShaderValueData value = null)
        {
            if(value != null)
                defaultValueData = value;

            this.controlData = new ShaderControlData()
            {
                labels = new string[] { label }
            };
        }

        public VisualElement GetControl(IShaderValue shaderValue)
        {
            VisualElement control = new VisualElement() { name = "LabelControl" };
            control.styleSheets.Add(Resources.Load<StyleSheet>("Styles/ShaderControls/LabelControl"));

            Label label = new Label(controlData.labels[0]);
            control.Add(label);
            return control;
        }
    }
}
