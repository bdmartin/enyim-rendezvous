// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Running;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
