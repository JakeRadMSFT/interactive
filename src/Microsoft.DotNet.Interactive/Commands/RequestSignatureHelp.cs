﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Interactive.Parsing;

namespace Microsoft.DotNet.Interactive.Commands
{
    public class RequestSignatureHelp : LanguageServiceCommand
    {
        public RequestSignatureHelp(
            string code,
            LinePosition linePosition,
            string targetKernelName = null)
            : base(code, linePosition, targetKernelName)
        {
        }

        internal RequestSignatureHelp(
            LanguageNode languageNode,
            LinePosition linePosition,
            KernelCommand parent = null)
            : base(languageNode, linePosition, parent)
        {
        }

        internal override LanguageServiceCommand With(
            LanguageNode languageNode,
            LinePosition position)
        {
            return new RequestSignatureHelp(languageNode, position, Parent);
        }
    }
}
