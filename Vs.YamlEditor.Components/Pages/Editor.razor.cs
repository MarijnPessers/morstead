﻿using BlazorMonaco;
using BlazorMonaco.Bridge;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
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
        private string Value { get; set; }
        private string TypeOfContent { get; set; }
        private CancellationTokenSource TokenSource { get; set; }

        private readonly string _language = "yaml";
        private TimeSpan _submitWait = TimeSpan.FromMilliseconds(5000);

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

        private void StartSubmitCountdown()
        {
            if (TokenSource != null)
            {
                TokenSource.Cancel();
            }
            TokenSource = new CancellationTokenSource();
            var ct = TokenSource.Token;
            var task = Task.Run(() =>
                {
                    Thread.Sleep(_submitWait);
                    ct.ThrowIfCancellationRequested();
                    SubmitPage();
                }
                , TokenSource.Token);
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

            //DebugRuleYamlFromContentResponse response = null;
            IEnumerable<FormattingException> formattingExceptions = new List<FormattingException>();
            try
            {
                var response = await client.DebugRuleYamlContentsAsync(new DebugRuleYamlFromContentRequest { Yaml = await _monacoEditor.GetValue() });
                formattingExceptions = response.ParseResult.FormattingExceptions;
            }
            catch (ApiException ex)
            {
                if (ex.StatusCode != 404)
                {
                    throw ex;
                }
                ResetErrors();
                return;
            }

            ResetErrors();
            var deltaDecorations = new List<ModelDeltaDecoration>();
            foreach (var exception in formattingExceptions)
            {
                var message = exception.Message;
                var range = new BlazorMonaco.Bridge.Range()
                {
                    StartLineNumber = exception.DebugInfo.Start.Line,
                    StartColumn = exception.DebugInfo.Start.Col,
                    EndLineNumber = exception.DebugInfo.End.Line,
                    EndColumn = exception.DebugInfo.End.Col
                };

                deltaDecorations.Add(await BuildDeltaDecoration(range, message));
            }

            MonacoController.SetDeltaDecorations(deltaDecorations.ToArray());
        }


        private async Task<ModelDeltaDecoration> BuildDeltaDecoration(BlazorMonaco.Bridge.Range range, string message )
        {
            var isWholeLine = false;

            range.StartLineNumber = Math.Max(range.StartLineNumber, 1);
            range.StartColumn = Math.Max(range.StartColumn, 1);
            range.EndLineNumber = Math.Max(range.EndLineNumber, 1);
            if (range.EndColumn == 0)
            {
                range.EndColumn = range.StartColumn;
                var content = await _monacoEditor.GetValue();
                var contentLines = content.Split("\n");
                range.EndColumn = (contentLines.ElementAt(Math.Min(contentLines.Length - 1, range.EndLineNumber - 1))?.Trim().Length ?? 0) + 1;
                isWholeLine = true;
            }

            var options = new ModelDecorationOptions {
                IsWholeLine = isWholeLine,
                InlineClassName = "editorError",
                InlineClassNameAffectsLetterSpacing = false,
                ClassName = "editorError",
                HoverMessage = new MarkdownString[] { new MarkdownString { Value = $"**Error**\r\n\r\n{message}" } },
                GlyphMarginClassName = "editorErrorGlyph",
                GlyphMarginHoverMessage = new MarkdownString[] { new MarkdownString { Value = $"**Error**\r\n\r\n{message}" } }
            };

            return new ModelDeltaDecoration { Range = range, Options = options };
        }

        private void ResetErrors()
        {
            MonacoController.ResetDeltaDecorations();
        }
    }
}