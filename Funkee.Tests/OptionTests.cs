using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Funky.Tests;

public enum TestErrors
{
    OptionIsNone,
}

[TestClass]
public class OptionTests
{
    [TestMethod]
    public void Creation()
    {
        var     someString = "test";
        string? nullString = null;

        Assert.IsTrue( Option.Some( someString ).IsSome );
        Assert.ThrowsException<ArgumentNullException>( () => Option.Some( nullString ) );
        Assert.IsTrue( Option.SomeUnsafe( nullString ).IsSome );
        Assert.IsTrue( Option.NoneOf<string>().IsNone );
        Assert.IsTrue( Option.From( new int?( 0 ) ).IsSome );
        Assert.IsTrue( Option.From( (int?)null ).IsNone );
    }

    [TestMethod]
    public void Predicates()
    {
        var         x = Option.Some( 5 );
        Option<int> y = Option.None;

        Assert.IsTrue( x.IsSomeBy( i => i == 5 ) );
        Assert.AreEqual( x.Where( i => i  == 5 ), x );
        Assert.AreEqual( x.Where( i => i  != 5 ), y );
    }

    [TestMethod]
    public void Unwrapping()
    {
        var         x = Option.Some( 1 );
        Option<int> y = Option.None;

        Assert.ThrowsException<InvalidOperationException>( () => y.Expect( String.Empty ) );
        Assert.ThrowsException<InvalidOperationException>( () => y.Unwrap() );
        Assert.AreEqual( 1, x.Unwrap() );
        Assert.AreEqual( 2, y.Unwrap( 2 ) );
        Assert.AreEqual( 4, y.Unwrap( OptionTests.GetRandomValue ) );
        Assert.IsTrue( x.TryUnwrap( out var i ) );
        Assert.AreEqual( 1, i );
        Assert.IsFalse( y.TryUnwrap( out _ ) );
    }

    [TestMethod]
    public void Selectors()
    {
        var         x = Option.Some( 5 );
        Option<int> y = Option.None;

        Assert.AreEqual( 10, x.Select( i => i * 2 ) );
        Assert.AreEqual( 0,  y.Select( i => i, 0 ) );
        Assert.AreEqual( 4,  y.Select( i => i, OptionTests.GetRandomValue ) );
    }

    [TestMethod]
    public void Logic()
    {
        var         five    = Option.Some( 5 );
        Option<int> noneInt = Option.None;

        Assert.IsTrue( five.IsSome );
        Assert.IsTrue( noneInt.IsNone );

        Assert.IsTrue( five.OkOr( TestErrors.OptionIsNone ).IsOk );
        Assert.IsTrue( noneInt.OkOr( () => TestErrors.OptionIsNone ).IsError );

        var ten = Option.Some( 10 );

        Assert.AreEqual( 10, five.And( ten ).Unwrap() );
        Assert.IsTrue( noneInt.And( five ).IsNone );
        Assert.IsTrue( five.And( noneInt ).IsNone );

        Assert.AreEqual( 10, five.And( () => ten ).Unwrap() );
        Assert.IsTrue( noneInt.And( () => five ).IsNone );
        Assert.IsTrue( five.And( () => noneInt ).IsNone );

        Assert.AreEqual( 5, five.Or( ten ).Unwrap() );
        Assert.IsTrue( five.Or( noneInt ).IsSome );
        Assert.IsTrue( noneInt.Or( five ).IsSome );

        Assert.AreEqual( 5, five.Or( () => ten ).Unwrap() );
        Assert.IsTrue( five.Or( () => noneInt ).IsSome );
        Assert.IsTrue( noneInt.Or( () => five ).IsSome );

        Assert.IsTrue( five.Xor( noneInt ).IsSome );
        Assert.IsTrue( five.Xor( ten ).IsNone );
        Assert.IsTrue( noneInt.Xor( five ).IsSome );
        Assert.IsTrue( noneInt.Xor( Option.None ).IsNone );

        Assert.IsTrue( five.Contains( 5 ) );
    }

    [TestMethod]
    public void Linq()
    {
        var         five    = Option.Some( 5 );
        var         ten     = Option.Some( 10 );
        Option<int> noneInt = Option.None;

        var a = from num in five
                where num == 5
                select num;

        var b = from num in noneInt
                where num == 5
                select num;

        Assert.AreEqual( a, Option.Some( 5 ) );
        Assert.AreEqual( b, Option.None );

        var zipped = five.Zip( ten );
        Assert.IsTrue( zipped.IsSome );
        Assert.AreEqual( 5,  zipped.Unwrap().Left );
        Assert.AreEqual( 10, zipped.Unwrap().Right );

        Assert.IsTrue( five.Zip( noneInt ).IsNone );

        var zip2 = five.Zip( ten, ( x, y ) => new[] { x, y } );
        Assert.IsTrue( zip2.IsSome );
        Assert.AreEqual( 5,  zip2.Unwrap()[0] );
        Assert.AreEqual( 10, zip2.Unwrap()[1] );
    }

    [TestMethod]
    public void Matching()
    {
        var         x = Option.Some( 5 );
        Option<int> y = Option.None;

        x.Match(
            i => Assert.AreEqual( 5, i ),
            Assert.Fail
        );

        Assert.AreEqual( 5, x.Match( i => i, () => 10 ) );

        y.Match(
            _ => Assert.Fail(),
            () => Assert.IsTrue( true )
        );

        Assert.AreEqual( 10, y.Match( i => i, () => 10 ) );
    }

    public static int GetRandomValue() => 4;
}