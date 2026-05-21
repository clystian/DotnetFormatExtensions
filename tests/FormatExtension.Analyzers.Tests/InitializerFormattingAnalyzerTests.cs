using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<Formatextension.Analyzers.InitializerFormattingAnalyzer>;

namespace Formatextension.Analyzers.Tests;

public class InitializerFormattingAnalyzerTests
{
    [Fact]
    public async Task SingleElement_NoDiagnostic()
    {
        var test = """
            using System.Collections.Generic;

            public class C
            {
                public void M()
                {
                    var dict = new Dictionary<string, int> { ["a"] = 1 };
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task TwoElementsOnSameLine_Diagnostic()
    {
        var test = """
            using System.Collections.Generic;

            public class C
            {
                public void M()
                {
                    var dict = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };
                }
            }
            """;

        var expected = VerifyCS.Diagnostic("FMT0001")
            .WithSpan(7, 48, 7, 49);

        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task AlreadyMultiLine_NoDiagnostic()
    {
        var test = """
            using System.Collections.Generic;

            public class C
            {
                public void M()
                {
                    var dict = new Dictionary<string, int>
                    {
                        ["a"] = 1,
                        ["b"] = 2
                    };
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ArrayInitializerOnSameLine_Diagnostic()
    {
        var test = """
            public class C
            {
                public void M()
                {
                    var arr = new int[] { 1, 2, 3 };
                }
            }
            """;

        var expected = VerifyCS.Diagnostic("FMT0001")
            .WithSpan(5, 29, 5, 30);

        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task CollectionInitializerOnSameLine_Diagnostic()
    {
        var test = """
            using System.Collections.Generic;

            public class C
            {
                public void M()
                {
                    var list = new List<int> { 1, 2, 3 };
                }
            }
            """;

        var expected = VerifyCS.Diagnostic("FMT0001")
            .WithSpan(7, 34, 7, 35);

        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task EmptyInitializer_NoDiagnostic()
    {
        var test = """
            using System.Collections.Generic;

            public class C
            {
                public void M()
                {
                    var dict = new Dictionary<string, int> { };
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task NestedInitializer_InnerOnSameLine_Diagnostic()
    {
        var test = """
            using System.Collections.Generic;

            public class C
            {
                public void M()
                {
                    var nested = new Dictionary<string, Dictionary<string, int>>
                    {
                        ["inner"] = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 }
                    };
                }
            }
            """;

        var expected = VerifyCS.Diagnostic("FMT0001")
            .WithSpan(9, 53, 9, 54);

        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task MixedLineInitializer_Diagnostic()
    {
        var test = """
            using System.Collections.Generic;

            public class C
            {
                public void M()
                {
                    var dict = new Dictionary<string, int>
                    {
                        ["a"] = 1, ["b"] = 2,
                        ["c"] = 3
                    };
                }
            }
            """;

        var expected = VerifyCS.Diagnostic("FMT0001")
            .WithSpan(8, 9, 8, 10);

        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task MultidimensionalArray_InnerInitializers_NoDiagnostic()
    {
        var test = """
            public class C
            {
                public void M()
                {
                    var matrix = new double[2, 3]
                    {
                        { 1.1, 1.2, 1.3 },
                        { 2.1, 2.2, 2.3 }
                    };
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task JaggedArray_InnerSameLine_Diagnostic()
    {
        var test = """
            public class C
            {
                public void M()
                {
                    var jagged = new int[][] { new int[] { 1, 2, 3 } };
                }
            }
            """;

        var expected = VerifyCS.Diagnostic("FMT0001")
            .WithSpan(5, 46, 5, 47);

        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task ImplicitObjectCreation_InitializerOnSameLine_Diagnostic()
    {
        var test = """
            public class C
            {
                public int Id { get; set; }
                public string Code { get; set; }

                public void M()
                {
                    var obj = new C() { Id = 1, Code = "A" };
                }
            }
            """;

        var expected = VerifyCS.Diagnostic("FMT0001")
            .WithSpan(8, 27, 8, 28);

        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }
}
