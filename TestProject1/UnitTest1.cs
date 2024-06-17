namespace TestProject1;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        ReadOnlySpan<int> a = [1, 2, 3];
        ReadOnlySpan<char> j = "dafdasdf";

        Span<char> k = ['a', 'b', 'c'];
        k.Fill(' ');
        
        var aa = string.Create(40, 40, (s, i) =>
        {
            s[0] = i.ToString().First();
        });
    }
}

public ref struct A
{
    public ReadOnlySpan<int> Value { get; set; }
}