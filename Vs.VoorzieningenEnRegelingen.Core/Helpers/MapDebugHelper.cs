﻿using System;
using System.Collections.Generic;
using System.Text;
using Vs.Core.Diagnostics;
using YamlDotNet.Core;

namespace Vs.VoorzieningenEnRegelingen.Core.Helpers
{
    public static class MapDebugHelper
    {
        public static DebugInfo MapDebugInfo(this DebugInfo debugInfo, Mark start, Mark end)
        {
            if (start is null)
            {
                throw new ArgumentNullException(nameof(start));
            }

            if (end is null)
            {
                throw new ArgumentNullException(nameof(end));
            }

            return new DebugInfo(
                start: new LineInfo(line: start.Line, col: start.Column, index: start.Index),
                end: new LineInfo(line: end.Line, col: end.Column, index: end.Index)
            );
        }
    }
}