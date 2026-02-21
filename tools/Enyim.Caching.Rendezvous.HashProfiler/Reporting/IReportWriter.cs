// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

namespace Enyim.Caching.Rendezvous.HashProfiler.Reporting;

public interface IReportWriter
{
    void Write(ProfileReport report, string outputDir);
}
