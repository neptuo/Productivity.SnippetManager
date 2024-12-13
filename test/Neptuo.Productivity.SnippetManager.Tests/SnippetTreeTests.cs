using Neptuo.Productivity.SnippetManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace Neptuo.Productivity.SnippetManager.Tests;

public class SnippetTreeTests
{
	public SnippetProviderContext GetTree(Action<Func<string, SnippetModel>>? capture = null)
	{
        var all = new List<SnippetModel>();
		var context = new SnippetProviderContext();

        void Add(string text)
        {
            var model = new SnippetModel(text, text);
            context.Add(model);
            all.Add(model);
        }

        Add("A - A - A");
		Add("A - A - B");
		Add("A - A - C");
		Add("A - A - D");
		Add("A - B - A");
		Add("A - B - B");
		Add("A - B - C");
		Add("A - C - A");
		Add("A - C - B");

        capture?.Invoke(title => all.Find(s => s.Title == title) ?? throw Ensure.Exception.InvalidOperation($"Snippet '{title}' not found"));

        return context;
    }

	[Fact]
	public void EnsureTree()
	{
        var tree = GetTree();
		var roots = tree.GetRoots();
		Assert.Collection(
			roots,
			a1 =>
			{
				Assert.Equal("A", a1.Title);
				Assert.True(a1.IsShadow);
				Assert.Null(tree.FindParent(a1));

				Assert.Collection(
					tree.GetChildren(a1),
                    a2 =>
                    {
                        Assert.Equal("A", a2.Title);
                        Assert.True(a2.IsShadow);
						Assert.Equal(a1, tree.FindParent(a2));

						Assert.Collection(
							tree.GetChildren(a2),
							a3 =>
							{
								Assert.False(a3.IsShadow);
                                Assert.Equal("A - A - A", a3.Title);
                                Assert.Equal(a2, tree.FindParent(a3));
                            },
                            b3 =>
                            {
                                Assert.False(b3.IsShadow);
                                Assert.Equal("A - A - B", b3.Title);
                                Assert.Equal(a2, tree.FindParent(b3));
                            },
                            c3 =>
                            {
                                Assert.False(c3.IsShadow);
                                Assert.Equal("A - A - C", c3.Title);
                                Assert.Equal(a2, tree.FindParent(c3));
                            },
                            d3 =>
                            {
                                Assert.False(d3.IsShadow);
                                Assert.Equal("A - A - D", d3.Title);
                                Assert.Equal(a2, tree.FindParent(d3));
                            }
                        );
                    },
                    b2 =>
                    {
                        Assert.Equal("B", b2.Title);
                        Assert.True(b2.IsShadow);
                        Assert.Equal(a1, tree.FindParent(b2));

                        Assert.Collection(
                            tree.GetChildren(b2),
                            a3 =>
                            {
                                Assert.False(a3.IsShadow);
                                Assert.Equal("A - B - A", a3.Title);
                                Assert.Equal(b2, tree.FindParent(a3));
                            },
                            b3 =>
                            {
                                Assert.False(b3.IsShadow);
                                Assert.Equal("A - B - B", b3.Title);
                                Assert.Equal(b2, tree.FindParent(b3));
                            },
                            c3 =>
                            {
                                Assert.False(c3.IsShadow);
                                Assert.Equal("A - B - C", c3.Title);
                                Assert.Equal(b2, tree.FindParent(c3));
                            }
                        );
                    },
                    c2 =>
                    {
                        Assert.Equal("C", c2.Title);
                        Assert.True(c2.IsShadow);
                        Assert.Equal(a1, tree.FindParent(c2));

                        Assert.Collection(
                            tree.GetChildren(c2),
                            a3 =>
                            {
                                Assert.False(a3.IsShadow);
                                Assert.Equal("A - C - A", a3.Title);
                                Assert.Equal(c2, tree.FindParent(a3));
                            },
                            b3 =>
                            {
                                Assert.False(b3.IsShadow);
                                Assert.Equal("A - C - B", b3.Title);
                                Assert.Equal(c2, tree.FindParent(b3));
                            }
                        );
                    }
                );
            }
		);
	}

    [Fact]
    public void RemoveAllChildren_RemovesShadowParent()
    {
        SnippetModel? aca = null;
        SnippetModel? acb = null;

        var tree = GetTree(capture =>
        {
            aca = capture("A - C - A");
            acb = capture("A - C - B");
        });

        tree.Remove(aca!);

        var ac = tree.GetChildren(tree.GetRoots().Single(s => s.Title == "A")).Single(s => s.Title == "C");
        var acChildren = tree.GetChildren(ac);

        Assert.Collection(
            acChildren,
            b3 =>
            {
                Assert.False(b3.IsShadow);
                Assert.Equal("A - C - B", b3.Title);
                Assert.Equal(ac, tree.FindParent(b3));
            }
        );

        tree.Remove(acb!);

        ac = tree.GetChildren(tree.GetRoots().Single(s => s.Title == "A")).SingleOrDefault(s => s.Title == "C");
        Assert.Null(ac);
    }

    [Fact]
    public void RemoveParent_RemovesAllAncestors()
    {
        var ac = new SnippetModel("A - C", "A - C");
        var tree = GetTree();

        tree.Add(new("A - C - D"));
        tree.Add(new("A - C - A - A"));

        tree.Add(ac);
        tree.Remove(ac);

        Assert.Null(tree.Models.FirstOrDefault(s => s.Title == "A - C - A"));
        Assert.Null(tree.Models.FirstOrDefault(s => s.Title == "A - C - B"));
        Assert.Null(tree.Models.FirstOrDefault(s => s.Title == "A - C - D"));
        Assert.Null(tree.Models.FirstOrDefault(s => s.Title == "A - C - A - A"));
    }

    [Fact]
    public void ActualSnippetReplacesShadow()
    {
        var tree = GetTree();

        tree.Add(new("A - B", "A - B"));

        var aChildren = tree.GetChildren(tree.GetRoots().Single(s => s.Title == "A"));

        Assert.Collection(
            aChildren,
            a2 =>
            {
                Assert.Equal("A", a2.Title);
                Assert.True(a2.IsShadow);
            },
            b2 =>
            {
                Assert.Equal("A - B", b2.Title);
                Assert.False(b2.IsShadow);
            },
            c2 =>
            {
                Assert.Equal("C", c2.Title);
                Assert.True(c2.IsShadow);
            }
        );
    }
}
