﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.DotNet.Interactive.Commands;

#nullable enable
namespace Microsoft.DotNet.Interactive
{
    public static class KernelCommandExtensions
    {
        internal const string TokenKey = "token";
        internal const string IdKey = "command-id";
        internal const string PublishInternalEventsKey = "publish-internal-events";

        public static void PublishInternalEvents(
            this KernelCommand command)
        {
            command.Properties[PublishInternalEventsKey] = true;
        }

        public static void SetToken(
            this KernelCommand command,
            string token)
        {
            if (!command.Properties.TryGetValue(TokenKey, out var existing))
            {
                command.Properties.Add(TokenKey, new TokenSequence(token));
            }
            else if (!(existing is TokenSequence sequence) || sequence.Current != token)
            {
                throw new InvalidOperationException("Command token cannot be changed.");
            }
        }

        public static string GetOrCreateToken(this KernelCommand command)
        {
            if (command is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            if (command.Properties.TryGetValue(TokenKey, out var value) &&
                value is TokenSequence tokenSequence)
            {
                return tokenSequence.Current;
            }

            if (command.Parent is { } parent)
            {
                var token = parent.GetOrCreateToken();
                command.SetToken(token);
                return token;
            }
            
            if (KernelInvocationContext.Current?.Command is { } contextCommand && 
                contextCommand != command)
            {
                var token = contextCommand.GetOrCreateToken();
                command.SetToken(token);
                return token;
            }

            return command.GenerateToken();
        }

        internal static void SetId(
            this KernelCommand command,
            string id)
        {
            command.Properties[IdKey] = id;
        }

        internal static string GetOrCreateId(this KernelCommand command)
        {
            if (command is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            if (command.Properties.TryGetValue(IdKey, out var value))
            {
                return (string)value;
            }

            var id = Guid.NewGuid().ToString("N");
            command.SetId(id);
            return id;

        }
        private static string GetNextToken(this KernelCommand command)
        {
            if (command.Properties.TryGetValue(TokenKey, out var value) &&
                value is TokenSequence tokenSequence)
            {
                return tokenSequence.GetNext();
            }

            return command.GenerateToken();
        }

        private static string GenerateToken(this KernelCommand command)
        {
            var seed = command.Parent?.GetNextToken();

            var sequence = new TokenSequence(seed);

            command.Properties.Add(TokenKey, sequence);

            return sequence.Current;
        }

        private class TokenSequence
        {
            private readonly object _lock = new();

            public TokenSequence(string? current = null)
            {
                Current = current ?? Hash(Guid.NewGuid().ToString());
            }

            internal string Current { get; private set; }

            public string GetNext()
            {
                string next;

                lock (_lock)
                {
                    next = Current = Hash(Current);
                }

                return next;
            }

            private static string Hash(string seed)
            {
                var inputBytes = Encoding.ASCII.GetBytes(seed);

                byte[] hash;
                using (var sha = SHA256.Create())
                {
                    hash = sha.ComputeHash(inputBytes);
                }

                return Convert.ToBase64String(hash);
            }
        }

        internal static bool IsEquivalentTo(this KernelCommand src, KernelCommand other)
        {
            return ReferenceEquals(src, other)
                   || src.GetOrCreateId() == other.GetOrCreateId();
        }
    }
}