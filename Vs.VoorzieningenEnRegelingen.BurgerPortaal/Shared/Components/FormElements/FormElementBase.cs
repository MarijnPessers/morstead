﻿using Microsoft.AspNetCore.Components;
using Vs.Cms.Core.Controllers.Interfaces;
using Vs.VoorzieningenEnRegelingen.BurgerPortaal.Objects.FormElements;
using Vs.VoorzieningenEnRegelingen.BurgerPortaal.Objects.FormElements.Interfaces;
using Vs.VoorzieningenEnRegelingen.BurgerPortaal.Shared.Components.FormElements.Interface;
using Vs.VoorzieningenEnRegelingen.Core;

namespace Vs.VoorzieningenEnRegelingen.BurgerPortaal.Shared.Components.FormElements
{
    public class FormElementBase : ComponentBase, IFormElementBase
    {
        private IFormElementData _data;

        [CascadingParameter]
        public IFormElementData CascadedData { get; set; }

        /// <summary>
        /// Needed for testing
        /// Uses Cascasded Data only if available
        /// </summary>
        [Parameter]
        public IFormElementData Data
        {
            get => CascadedData != null ? CascadedData : _data;
            set => _data = value;
        }

        [Parameter]
        public virtual string Value { get => Data.Value; set => Data.Value = value; }

        public bool ShowElement => Data != null && !string.IsNullOrWhiteSpace(Data.Name);

        public virtual void FillDataFromResult(IExecutionResult result, IContentController contentController)
        {
            //todo MPS write test
            Data = new FormElementData();
            if (result.InferedType != TypeInference.InferenceResult.TypeEnum.Unknown)
            {
                Data.FillFromExecutionResult(result, contentController);
            }
        }

        public IFormElementBase GetFormElement(IExecutionResult result)
        {
            switch (result.InferedType)
            {
                case TypeInference.InferenceResult.TypeEnum.Double:
                    return new Number();
                case TypeInference.InferenceResult.TypeEnum.Boolean:
                    return new Radio();
                case TypeInference.InferenceResult.TypeEnum.List:
                    return new Select();
                case TypeInference.InferenceResult.TypeEnum.TimeSpan:
                case TypeInference.InferenceResult.TypeEnum.DateTime:
                case TypeInference.InferenceResult.TypeEnum.String:
                case TypeInference.InferenceResult.TypeEnum.Period:
                case TypeInference.InferenceResult.TypeEnum.Unknown:
                default:
                    return this;
            }
        }
    }
}