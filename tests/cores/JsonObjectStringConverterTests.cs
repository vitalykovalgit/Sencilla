namespace Sencilla.Core.Tests;

/// <summary>
/// <see cref="JsonObjectStringConverter"/> — the bridge between a column that HOLDS json text and a property
/// that IS json on the wire.
///
/// The regression that motivated these: a mis-shaped value used to be skipped and read back as null, so the
/// request succeeded with 200 OK while the column was silently wiped. That cost a real row
/// (Element.Attrs) during testing. A malformed body must throw; it must never quietly destroy data.
/// </summary>
public class JsonObjectStringConverterTests
{
    private class Holder
    {
        [JsonObjectString]
        public string? Attrs { get; set; }
    }

    /// <summary>
    /// Web defaults, as the API uses them: camelCase on the wire, case-insensitive on the way in. The
    /// converter is wired by <see cref="JsonObjectStringAttribute"/> on the property, exactly as on a real
    /// entity — not registered here, so the test exercises the same path production does.
    /// </summary>
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    private static Holder Read(string json) => JsonSerializer.Deserialize<Holder>(json, Options)!;
    private static string Write(string? attrs) => JsonSerializer.Serialize(new Holder { Attrs = attrs }, Options);

    // ---- reading: the shapes a client legitimately sends ----

    [Fact]
    public void Object_IsStoredAsItsRawText()
        => Assert.Equal("""{"a":1}""", Read("""{"attrs":{"a":1}}""").Attrs);

    [Fact]
    public void Array_IsStoredAsItsRawText()
        => Assert.Equal("[1,2]", Read("""{"attrs":[1,2]}""").Attrs);

    [Fact]
    public void ExplicitNull_ClearsTheColumn()
        => Assert.Null(Read("""{"attrs":null}""").Attrs);

    /// <summary>The property is a string in C# and in the DB, so serializers naturally emit one.</summary>
    [Fact]
    public void JsonSentAsAString_IsAccepted()
        => Assert.Equal("""{"a":1}""", Read("""{"attrs":"{\"a\":1}"}""").Attrs);

    [Fact]
    public void EmptyString_ClearsTheColumn()
        => Assert.Null(Read("""{"attrs":""}""").Attrs);

    /// <summary>Verbatim, not round-tripped through Dictionary&lt;string, object&gt;.</summary>
    [Fact]
    public void NestedValues_SurviveUnchanged()
        => Assert.Equal("""{"n":{"deep":[1,null,2.5]},"b":true}""",
                        Read("""{"attrs":{"n":{"deep":[1,null,2.5]},"b":true}}""").Attrs);

    // ---- reading: the shapes that must FAIL rather than wipe the column ----

    [Theory]
    [InlineData(""" {"attrs":42} """)]
    [InlineData(""" {"attrs":true} """)]
    [InlineData(""" {"attrs":"not json at all"} """)]
    [InlineData(""" {"attrs":"<xml/>"} """)]
    public void MalformedValue_Throws_InsteadOfSilentlyNullingTheColumn(string json)
        => Assert.Throws<JsonException>(() => Read(json.Trim()));

    // ---- writing ----

    [Fact]
    public void StoredJson_IsWrittenBackAsAnObject()
        => Assert.Equal("""{"attrs":{"a":1}}""", Write("""{"a":1}"""));

    [Fact]
    public void StoredArray_IsWrittenBackAsAnArray()
        => Assert.Equal("""{"attrs":[1,2]}""", Write("[1,2]"));

    /// <summary>
    /// Writing nothing used to leave the writer expecting a value it never got, throwing mid-response —
    /// a 500 on a plain GET of a row whose json column happened to be blank.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankColumn_IsWrittenAsNull_NotOmitted(string stored)
        => Assert.Equal("""{"attrs":null}""", Write(stored));

    /// <summary>A legacy row holding non-json text must still be readable, not fail the whole response.</summary>
    [Fact]
    public void LegacyNonJsonText_IsWrittenAsAString()
        => Assert.Equal("""{"attrs":"plain text"}""", Write("plain text"));

    // ---- the round trip the API actually performs ----

    [Fact]
    public void ObjectIn_ObjectOut_SurvivesARoundTrip()
    {
        var stored = Read("""{"attrs":{"sizes":{},"prodtime":{"min":1,"max":2}}}""").Attrs;
        Assert.Equal("""{"attrs":{"sizes":{},"prodtime":{"min":1,"max":2}}}""", Write(stored));
    }

    /// <summary>
    /// Several of these columns are NVARCHAR(4000); an indented body must not be stored at several times
    /// the size of the same data compacted.
    /// </summary>
    [Fact]
    public void IndentedInput_IsCompactedBeforeStorage()
        => Assert.Equal("""{"a":1,"b":[1,2]}""",
                        Read("{\"attrs\": {\n  \"a\" : 1,\n  \"b\" : [ 1, 2 ]\n} }").Attrs);

    [Fact]
    public void IndentedJsonSentAsAString_IsAlsoCompacted()
        => Assert.Equal("""{"a":1}""", Read("""{"attrs":"{\n  \"a\" : 1\n}"}""").Attrs);
}
