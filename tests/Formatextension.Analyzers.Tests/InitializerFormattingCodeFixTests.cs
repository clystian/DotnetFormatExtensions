using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.CodeFixVerifier<
    Formatextension.Analyzers.InitializerFormattingAnalyzer,
    Formatextension.Analyzers.InitializerFormattingCodeFix>;

namespace Formatextension.Analyzers.Tests;

public class InitializerFormattingCodeFixTests
{
    [Fact]
    public async Task TwoElementsOnSameLine_FixesToSeparateLines()
    {
        var before = """
            using System.Collections.Generic;

            public class C
            {
                public void M()
                {
                    var dict = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };
                }
            }
            """;

        var after = """
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

        var expected = VerifyCS.Diagnostic("FMT0001")
            .WithSpan(7, 48, 7, 49);

        await VerifyCS.VerifyCodeFixAsync(before, expected, after);
    }

    [Fact]
    public async Task ThreeElementsOnSameLine_FixesToSeparateLines()
    {
        var before = """
            public class C
            {
                public void M()
                {
                    var arr = new int[] { 1, 2, 3 };
                }
            }
            """;

        var after = """
            public class C
            {
                public void M()
                {
                    var arr = new int[]
                    {
                        1,
                        2,
                        3
                    };
                }
            }
            """;

        var expected = VerifyCS.Diagnostic("FMT0001")
            .WithSpan(5, 29, 5, 30);

        await VerifyCS.VerifyCodeFixAsync(before, expected, after);
    }

    [Fact]
    public async Task AlreadyMultiLine_NoChange()
    {
        var before = """
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

        await VerifyCS.VerifyCodeFixAsync(before, before);
    }

    [Fact]
    public async Task NestedInitializer_FixesBothLevels()
    {
        var before = """
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

        var after = """
            using System.Collections.Generic;

            public class C
            {
                public void M()
                {
                    var nested = new Dictionary<string, Dictionary<string, int>>
                    {
                        ["inner"] = new Dictionary<string, int>
                        {
                            ["a"] = 1,
                            ["b"] = 2
                        }
                    };
                }
            }
            """;

        var expected = VerifyCS.Diagnostic("FMT0001")
            .WithSpan(9, 53, 9, 54);

        await VerifyCS.VerifyCodeFixAsync(before, expected, after);
    }

    [Fact]
    public async Task MixedLineInitializer_FixesToFullyMultiLine()
    {
        var before = """
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

        var after = """
            using System.Collections.Generic;

            public class C
            {
                public void M()
                {
                    var dict = new Dictionary<string, int>
                    {
                        ["a"] = 1,
                        ["b"] = 2,
                        ["c"] = 3
                    };
                }
            }
            """;

        var expected = VerifyCS.Diagnostic("FMT0001")
            .WithSpan(8, 9, 8, 10);

        await VerifyCS.VerifyCodeFixAsync(before, expected, after);
    }

    [Fact]
    public async Task MultidimensionalArray_InnerNotExpanded()
    {
        var before = """
            public class C
            {
                public void M()
                {
                    var matrix = new double[2, 2] { { 1.1, 1.2 }, { 2.1, 2.2 } };
                }
            }
            """;

        var after = """
            public class C
            {
                public void M()
                {
                    var matrix = new double[2, 2]
                    {
                        { 1.1, 1.2 },
                        { 2.1, 2.2 }
                    };
                }
            }
            """;

        var expected = VerifyCS.Diagnostic("FMT0001")
            .WithSpan(5, 39, 5, 40);

        await VerifyCS.VerifyCodeFixAsync(before, expected, after);
    }

    [Fact]
    public async Task JaggedArray_InnerExpanded()
    {
        var before = """
            public class C
            {
                public void M()
                {
                    var jagged = new int[][] { new int[] { 1, 2, 3 }, new int[] { 4, 5 } };
                }
            }
            """;

        var after = """
            public class C
            {
                public void M()
                {
                    var jagged = new int[][]
                    {
                        new int[] 
                        {
                            1,
                            2,
                            3
                        },
                        new int[] 
                        {
                            4,
                            5
                        }
                    };
                }
            }
            """;

        var expected1 = VerifyCS.Diagnostic("FMT0001")
            .WithSpan(5, 34, 5, 35);
        var expected2 = VerifyCS.Diagnostic("FMT0001")
            .WithSpan(5, 46, 5, 47);
        var expected3 = VerifyCS.Diagnostic("FMT0001")
            .WithSpan(5, 69, 5, 70);

        await VerifyCS.VerifyCodeFixAsync(before, [expected1, expected2, expected3], after);
    }

    [Fact]
    public async Task ImplicitObjectCreation_FixesNested()
    {
        var before = """
            using System.Collections.Generic;

            public class Node
            {
                public int Id { get; set; }
                public double[] Thresholds { get; set; }
            }

            public class C
            {
                public void M()
                {
                    var nodes = new List<Node> { new() { Id = 1, Thresholds = new[] { 0.15, 0.24 } } };
                }
            }
            """;

        var after = """
            using System.Collections.Generic;

            public class Node
            {
                public int Id { get; set; }
                public double[] Thresholds { get; set; }
            }

            public class C
            {
                public void M()
                {
                    var nodes = new List<Node> { new()
                    {
                        Id = 1,
                        Thresholds = new[] 
                        {
                            0.15,
                            0.24
                        }
                    } };
                }
            }
            """;

        var expected1 = VerifyCS.Diagnostic("FMT0001")
            .WithSpan(13, 44, 13, 45);
        var expected2 = VerifyCS.Diagnostic("FMT0001")
            .WithSpan(13, 73, 13, 74);

        await VerifyCS.VerifyCodeFixAsync(before, [expected1, expected2], after);
    }

    [Fact]
    public async Task DeeplyNested_FixesAllLevels()
    {
        var before = """
            using System.Collections.Generic;

            public class Config
            {
                public List<string> Channels { get; set; }
            }

            public class Node
            {
                public int Id { get; set; }
                public Config Config { get; set; }
            }

            public class C
            {
                public void M()
                {
                    var graph = new Dictionary<string, Node>
                    {
                        ["n1"] = new Node { Id = 101, Config = new Config { Channels = new List<string> { "A", "B" } } }
                    };
                }
            }
            """;

        var after = """
            using System.Collections.Generic;

            public class Config
            {
                public List<string> Channels { get; set; }
            }

            public class Node
            {
                public int Id { get; set; }
                public Config Config { get; set; }
            }

            public class C
            {
                public void M()
                {
                    var graph = new Dictionary<string, Node>
                    {
                        ["n1"] = new Node
                        {
                            Id = 101,
                            Config = new Config 
                            {
                                Channels = new List<string> 
                                {
                                    "A",
                                    "B"
                                }
                            }
                        }
                    };
                }
            }
            """;

        var expected1 = VerifyCS.Diagnostic("FMT0001")
            .WithSpan(20, 31, 20, 32);
        var expected2 = VerifyCS.Diagnostic("FMT0001")
            .WithSpan(20, 93, 20, 94);

        await VerifyCS.VerifyCodeFixAsync(before, [expected1, expected2], after);
    }
}
