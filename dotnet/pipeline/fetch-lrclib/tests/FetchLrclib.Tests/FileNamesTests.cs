namespace FetchLrclib.Tests;

public class FileNamesTests
{
    [Theory]
    [InlineData("Guns N' Roses", "Sweet Child O' Mine", "Guns N' Roses - Sweet Child O' Mine")]
    [InlineData("AC/DC", "a<b>c:d\"e\\f|g?h*i\tj", "AC_DC - a_b_c_d_e_f_g_h_i_j")]
    [InlineData("A", "Wait... ", "A - Wait")]
    public void BaseName_makes_a_safe_name(string artist, string track, string expected) =>
        Assert.Equal(expected, FileNames.BaseName(artist, track));
}
