using Xunit;

// AdminApiFactory (used by AdminAuthHttpTests) mutates process-wide environment variables while
// booting the real pipeline, because Program.cs reads Jwt:Key/ConnectionStrings:DefaultConnection
// out of configuration before builder.Build() runs - too early for WebApplicationFactory's normal
// ConfigureAppConfiguration override to reach. That makes those tests unsafe to run concurrently
// with anything else in this assembly; disabling parallelization for the whole assembly is the
// simplest way to guarantee that (xunit.runner.json's parallelizeTestCollections alone was not
// reliably honored for this project, so this stronger, explicitly-documented attribute is used).
[assembly: CollectionBehavior(DisableTestParallelization = true)]
