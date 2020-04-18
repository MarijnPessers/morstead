﻿using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Vs.Cms.Core.Controllers.Interfaces;
using Vs.Cms.Core.Helper;
using Vs.Cms.Core.Interfaces;
using Vs.Cms.Core.Objects.Interfaces;
using Vs.Core.Enums;
using Vs.Core.Extensions;
using Vs.Rules.Core;
using Vs.Rules.Core.Interfaces;

namespace Vs.Cms.Core.Controllers
{
    public class ContentController : IContentController
    {
        private readonly IRenderStrategy _renderStrategy;
        private IContentHandler _contentHandler;
        private readonly ITemplateEngine _templateEngine;
        private CultureInfo _cultureInfo;

        public IParametersCollection _parameters { get; set; }

        public ContentController(IRenderStrategy renderStrategy, IContentHandler contentHandler, ITemplateEngine templateEngine)
        {
            _renderStrategy = renderStrategy;
            _contentHandler = contentHandler;
            _templateEngine = templateEngine;
        }

        public void SetCulture(CultureInfo cultureInfo)
        {
            _cultureInfo = cultureInfo;
            if (_contentHandler != null)
            {
                _contentHandler.SetDefaultCulture(_cultureInfo);
            }
        }

        public string GetText(string semanticKey, FormElementContentType type, string defaultResult = null)
        {
            return GetText(semanticKey, type.GetDescription(), defaultResult);
        }

        public string GetText(string semanticKey, string type, string defaultResult = null)
        {
            var cultureContent = _contentHandler.GetDefaultContent();
            var template = cultureContent.GetContent(semanticKey, type);
            if (template == null)
            {
                return defaultResult;
            }
            return _renderStrategy.Render(template.ToString(), GetParametersDictionary());
        }

        private IDictionary<string, object> GetParametersDictionary()
        {
            var result = new Dictionary<string, object>();
            if (_parameters == null)
            {
                return result;
            }
            foreach (var parameter in _parameters)
            {
                if (result.ContainsKey(parameter.Name))
                {
                    //always take the last supplied value in the chain
                    if (parameter.Type == TypeInference.InferenceResult.TypeEnum.Double)
                    {
                        result[parameter.Name] = parameter.Value;
                    }
                    else
                    {
                        result[parameter.Name] = parameter.ValueAsString;
                    }

                    continue;
                }
                if (parameter.Type == TypeInference.InferenceResult.TypeEnum.Double)
                {
                    result.Add(parameter.Name, parameter.Value);
                }
                else
                {
                    result.Add(parameter.Name, parameter.ValueAsString);
                }
            }
            return result;
        }

        public void Initialize(string body)
        {
            //todo MPS Rewrite to get this from the body supplied
            _cultureInfo = new CultureInfo("nl-NL");
            _contentHandler.SetDefaultCulture(_cultureInfo);
            var parsedContent = YamlContentParser.RenderContentYamlToObject(body);
            _contentHandler.TranslateParsedContentToContent(_cultureInfo, parsedContent);
        }

        public IEnumerable<string> GetUnresolvedParameters(string semanticKey, IParametersCollection parameters)
        {
            _parameters = parameters;
            var texts = GetAllApplicableTexts(semanticKey);
            return GetUnresolvedParameters(texts);
        }

        private IEnumerable<string> GetAllApplicableTexts(string semanticKey)
        {
            var cultureContent = _contentHandler.GetDefaultContent();
            var objects = cultureContent.GetCompleteContent(semanticKey);
            return objects.Select(o => o.ToString());
        }

        private IEnumerable<string> GetUnresolvedParameters(IEnumerable<string> texts)
        {
            var needed = new List<string>();
            foreach (var text in texts)
            {
                needed.AddRange(_templateEngine.GetExpressionNames(text));
            }
            return needed.Except(_parameters.GetAll().Select(p => p.Name));
        }

        public void SetParameters(IParametersCollection parameters)
        {
            _parameters = parameters;
        }
    }
}
