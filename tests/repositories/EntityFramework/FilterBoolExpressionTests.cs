using System.Linq.Dynamic.Core;

namespace Sencilla.Repository.EntityFramework.Tests;

public class BoolFlagRow
{
    public int Id { get; set; }

    /// <summary>Non-nullable — the shape that broke. `bit NOT NULL` maps here.</summary>
    public bool Test { get; set; }

    /// <summary>Nullable twin, which always filtered correctly and must keep doing so.</summary>
    public bool? ProdAllowed { get; set; }
}

/// <summary>
/// A `bool` criterion must narrow the result set.
///
/// <para>`ToExpression` used to emit `Test in (False)`, and System.Linq.Dynamic.Core's
/// <c>ExpressionParser.ParseIn</c> seeds its OR accumulator with the LEFT operand, then asks
/// <c>accumulate.Type != typeof(bool)</c> to tell "nothing accumulated yet" apart from "keep OR-ing".
/// For a NON-nullable bool column the seed is already a bool expression, so the column itself was ORed
/// into the chain and the predicate compiled to <c>Test || Test == false</c> — a tautology that returned
/// every row. `bool?` slipped past the check, which is why the nullable flag beside it kept working, and
/// `in (True)` looked fine only because <c>Test || Test == true</c> is still <c>Test == true</c>.</para>
///
/// <para>These assert the compiled BEHAVIOUR through Dynamic LINQ, not the expression text — the text is
/// only wrong because of what the parser does with it.</para>
/// </summary>
public class FilterBoolExpressionTests
{
    private static readonly BoolFlagRow[] Rows =
    [
        new() { Id = 1, Test = true,  ProdAllowed = true  },
        new() { Id = 2, Test = false, ProdAllowed = false },
        new() { Id = 3, Test = false, ProdAllowed = null  },
    ];

    private static int[] Filter(string name, Type type, params object?[] values)
    {
        var prop = new FilterProperty { Query = name, Type = type };
        prop.AddValues(values);

        var expression = FilterConstraintHandler<BoolFlagRow>.ToExpression(prop)!;

        return Rows.AsQueryable().Where(expression).Select(r => r.Id).ToArray();
    }

    [Fact]
    public void False_on_a_non_nullable_bool_narrows()
        => Assert.Equal([2, 3], Filter(nameof(BoolFlagRow.Test), typeof(bool), false));

    [Fact]
    public void True_on_a_non_nullable_bool_narrows()
        => Assert.Equal([1], Filter(nameof(BoolFlagRow.Test), typeof(bool), true));

    [Fact]
    public void Both_values_match_every_row_because_they_genuinely_do()
        => Assert.Equal([1, 2, 3], Filter(nameof(BoolFlagRow.Test), typeof(bool), true, false));

    [Fact]
    public void False_on_a_nullable_bool_still_narrows()
        => Assert.Equal([2], Filter(nameof(BoolFlagRow.ProdAllowed), typeof(bool?), false));

    [Fact]
    public void True_on_a_nullable_bool_still_narrows()
        => Assert.Equal([1], Filter(nameof(BoolFlagRow.ProdAllowed), typeof(bool?), true));

    /// <summary>The `Values == [null]` short-circuit above the type switch keeps owning this.</summary>
    [Fact]
    public void Null_on_a_nullable_bool_selects_the_unset_row()
        => Assert.Equal([3], Filter(nameof(BoolFlagRow.ProdAllowed), typeof(bool?), [null]));

    /// <summary>Non-bool types keep the `in (...)` form; this is the guard against widening the fix.</summary>
    [Fact]
    public void Numbers_are_untouched_and_still_use_in()
    {
        var prop = new FilterProperty { Query = nameof(BoolFlagRow.Id), Type = typeof(int) };
        prop.AddValues(1, 3);

        Assert.Equal("Id in (1,3)", FilterConstraintHandler<BoolFlagRow>.ToExpression(prop));
        Assert.Equal([1, 3], Filter(nameof(BoolFlagRow.Id), typeof(int), 1, 3));
    }
}
