﻿using BlazorMonaco;
using BlazorMonaco.Bridge;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using Vs.YamlEditor.Components.Controllers.ApiCalls;
using Vs.YamlEditor.Components.Controllers.Interfaces;

namespace Vs.YamlEditor.Components.Pages
{
    public partial class Editor
    {
        [Inject]
        public IJSRuntime JsRuntime { get; set; }

        [Inject]
        public IMonacoController MonacoController { get; set; }

        private string Url { get; set; } = "https://raw.githubusercontent.com/sjefvanleeuwen/virtual-society-urukagina/master/Vs.VoorzieningenEnRegelingen.Core.TestData/YamlScripts/Zorgtoeslag5.yaml";
        private readonly string _language = "yaml";
        private string Value { get; set; }
        private string TypeOfContent { get; set; }

        private readonly IDictionary<string, bool> _types = new Dictionary<string, bool> {
            { "Rule", true },
            { "Content", false },
            { "Routing", false },
            { "Layer", false }
        };

        private MonacoEditor _monacoEditor { get; set; }

        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {
                MonacoController.Language = _language;
                MonacoController.MonacoEditor = _monacoEditor;
            }
            base.OnAfterRender(firstRender);
        }

        private StandaloneEditorConstructionOptions EditorConstructionOptions(MonacoEditor editor)
        {
            return new StandaloneEditorConstructionOptions
            {
                AutomaticLayout = true,
                Language = _language,
                GlyphMargin = true
            };
        }

        private bool GetEnabledForType(string type)
        {
            return _types.ContainsKey(type) && _types[type];
        }

        private string GetStyleForType(string type)
        {
            if (GetEnabledForType(type))
            {
                return string.Empty;
            }

            return "disabled";
        }

        private async void LoadUrl()
        {
            using (var client = new HttpClient())
            {
                try
                {
                    var response = await client.GetAsync(Url);
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            Value = reader.ReadToEnd();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Value = @$"Laden mislukt. Klopt de url?
Details:
{ex}";
                }
            }

            await _monacoEditor.SetValue(Value);
        }

        private async void SubmitPage()
        {
            if (string.IsNullOrWhiteSpace(TypeOfContent))
            {
                return;
            }

            var client = new RulesControllerDisciplClient(new HttpClient())
            {
                BaseUrl = "https://localhost:44391/"
            };

            var response = await client.DebugRuleYamlContentsAsync(new DebugRuleYamlFromContentRequest { Yaml = await _monacoEditor.GetValue() });
            ResetErrors();
            foreach (var exception in response.ParseResult.FormattingExceptions)
            {
                var message = exception.Message;
                var range = new BlazorMonaco.Bridge.Range()
                {
                    StartLineNumber = exception.DebugInfo.Start.Line,
                    StartColumn = exception.DebugInfo.Start.Col,
                    EndLineNumber = exception.DebugInfo.End.Line,
                    EndColumn = exception.DebugInfo.End.Col
                };
                
                SetError(range, "Error", message);
            }
        }

        private async void SetError(BlazorMonaco.Bridge.Range range, string title, string message )
        {
            var isWholeLine = false;

            range.StartLineNumber = Math.Max(range.StartLineNumber, 1);
            range.StartColumn = Math.Max(range.StartColumn, 1);
            range.EndLineNumber = Math.Max(range.EndLineNumber, 1);
            if (range.EndColumn == 0)
            {
                range.EndColumn = range.StartColumn;
                var content = await _monacoEditor.GetValue();
                range.EndColumn = (content.Split("\n").ElementAt(range.EndLineNumber)?.Trim().Length ?? 0) + 1;
                isWholeLine = true;
            }

            var options = new DecorationOptions {
                IsWholeLine = isWholeLine,
                InlineClassName = "editorError",
                InlineClassNameAffectsLetterSpacing = false,
                ClassName = "editorError",
                HoverMessage = new MarkdownString { Value = $"**{title}**\r\n\r\n{message}" },
                GlyphMarginClassName = "editorErrorGlyph",
                GlyphMarginHoverMessage = new MarkdownString { Value = $"**{title}**\r\n\r\n{message}" }
            };
            //TODO to enable after BlazorMonaco Update
            MonacoController.SetDeltaDecorations(range, options);
            //MonacoController.SetHoverText(range, title, message);
        }

        private void ResetErrors()
        {
            MonacoController.ResetDeltaDecorations();
            //MonacoController.ResetHoverTexts();
        }
    }
}
