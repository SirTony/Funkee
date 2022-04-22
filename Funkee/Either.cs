namespace Funkee;

public static class Either
{
    public readonly ref struct LeftValue<T>
    {
        internal readonly bool IsDefault;
        internal          T    Value { get; }

        public LeftValue()
        {
            this.IsDefault = true;
            this.Value     = default!;
        }

        internal LeftValue( T value )
        {
            this.IsDefault = false;
            this.Value     = value;
        }
    }

    public readonly ref struct RightValue<T>
    {
        internal readonly bool IsDefault;
        internal          T    right { get; }

        public RightValue()
        {
            this.IsDefault = true;
            this.right     = default!;
        }

        internal RightValue( T right )
        {
            this.IsDefault = false;
            this.right     = right;
        }
    }

    public static LeftValue<T>  Left<T>( T  value ) => new( value );
    public static RightValue<T> Right<T>( T right ) => new( right );
}

public abstract record class Either<TLeft, TRight>
{
    public sealed record class Left( TLeft Value ) : Either<TLeft, TRight>( true );

    public sealed record class Right( TRight Value ) : Either<TLeft, TRight>( false );

    public bool IsLeft  { get; }
    public bool IsRight => !this.IsLeft;

    private Either() => throw new InvalidOperationException();
    private Either( bool isLeft ) => this.IsLeft = isLeft;

    public bool IsLeftBy( Predicate<TLeft> predicate ) => this switch
    {
        Left { Value: var x } => predicate( x ),
        _                     => false,
    };

    public bool IsRightBy( Predicate<TRight> predicate ) => this switch
    {
        Right { Value: var x } => predicate( x ),
        _                      => false,
    };

    public TLeft UnwrapLeft() => this switch
    {
        Left { Value: var x } => x,
        _                     => throw new InvalidOperationException( "cannot unwrap a right value" ),
    };

    public TRight UnwrapRight() => this switch
    {
        Right { Value: var x } => x,
        _                      => throw new InvalidOperationException( "cannot unwrap a left value" ),
    };

    public TLeft UnwrapLeftOr( TLeft value ) => this switch
    {
        Left { Value: var x } => x,
        _                     => value,
    };

    public TRight UnwrapRightOr( TRight right ) => this switch
    {
        Right { Value: var x } => x,
        _                      => right,
    };

    public TLeft UnwrapLeftOr( Func<TLeft> valueFunc ) => this switch
    {
        Left { Value: var x } => x,
        _                     => valueFunc(),
    };

    public TRight UnwrapRightOr( Func<TRight> errorFunc ) => this switch
    {
        Right { Value: var x } => x,
        _                      => errorFunc(),
    };

    public bool TryUnwrapLeft( out TLeft value )
    {
        switch( this )
        {
            case Left { Value: var x }:
                value = x;
                return true;

            default:
                value = default!;
                return false;
        }
    }

    public bool TryUnwrapRight( out TRight right )
    {
        switch( this )
        {
            case Right { Value: var x }:
                right = x;
                return true;

            default:
                right = default!;
                return true;
        }
    }

    public bool ContainsLeft( TLeft value )
        => this.TryUnwrapLeft( out var x ) && EqualityComparer<TLeft>.Default.Equals( x, value );

    public bool ContainsRight( TRight right )
        => this.TryUnwrapRight( out var x ) && EqualityComparer<TRight>.Default.Equals( x, right );

    public Either<TValue2, TRight> SelectLeft<TValue2>( Func<TLeft, Either<TValue2, TRight>> selector ) => this switch
    {
        Left { Value : var x } => selector( x ),
        Right { Value: var x } => x,
        _                      => throw new NotImplementedException(),
    };

    public Either<TLeft, TRight2> SelectRight<TRight2>( Func<TRight, Either<TLeft, TRight2>> selector ) => this switch
    {
        Left { Value : var x } => x,
        Right { Value: var x } => selector( x ),
        _                      => throw new NotImplementedException(),
    };

    public TValue2 SelectLeft<TValue2>( Func<TLeft, TValue2> selector, TValue2 defaultValue ) => this switch
    {
        Left { Value: var x } => selector( x ),
        _                     => defaultValue,
    };

    public TRight2 SelectRight<TRight2>( Func<TRight, TRight2> selector, TRight2 defaultValue ) => this switch
    {
        Right { Value: var x } => selector( x ),
        _                      => defaultValue,
    };

    public TValue2 SelectLeft<TValue2>( Func<TLeft, TValue2> selector, Func<TValue2> defaultValueFunc ) => this switch
    {
        Left { Value: var x } => selector( x ),
        _                     => defaultValueFunc(),
    };

    public Either<TLeft, TRight2> SelectRight<TRight2>(
        Func<TRight, TRight2> selector,
        Func<TRight2>         defaultRightorFunc
    ) => this switch
    {
        Right { Value: var x } => selector( x ),
        _                      => defaultRightorFunc(),
    };

    public TLeft ExpectLeft( string msg )
        => this.TryUnwrapLeft( out var x ) ? x : throw new InvalidCastException( msg );

    public TRight ExpectRight( string msg )
        => this.TryUnwrapRight( out var x ) ? x : throw new InvalidCastException( msg );

    public Either<TLeft2, TRight> AndLeft<TLeft2>( Either<TLeft2, TRight> other ) => this switch
    {
        Left _                 => other,
        Right { Value: var x } => x,
        _                      => throw new NotImplementedException(),
    };

    public Either<TLeft2, TRight> AndLeft<TLeft2>( Func<Either<TLeft2, TRight>> otherFunc ) => this switch
    {
        Left _                 => otherFunc(),
        Right { Value: var x } => x,
        _                      => throw new NotImplementedException(),
    };

    public Either<TLeft, TRight2> AndRight<TRight2>( Either<TLeft, TRight2> other ) => this switch
    {
        Left { Value: var x } => x,
        Right _               => other,
        _                     => throw new NotImplementedException(),
    };

    public Either<TLeft, TRight2> AndRight<TRight2>( Func<Either<TLeft, TRight2>> otherFunc ) => this switch
    {
        Left { Value: var x } => x,
        Right _               => otherFunc(),
        _                     => throw new NotImplementedException(),
    };

    public void Match( Action<TLeft> left, Action<TRight> err )
    {
        switch( this )
        {
            case Left { Value: var v }:
                left( v );
                break;

            case Right { Value: var x }:
                err( x );
                break;
        }
    }

    public T Match<T>( Func<TLeft, T> left, Func<TRight, T> right ) => this switch
    {
        Left { Value : var l } => left( l ),
        Right { Value: var r } => right( r ),
        _                      => throw new NotImplementedException(),
    };

    public static implicit operator Either<TLeft, TRight>( in Either.LeftValue<TLeft> left ) => new Left( left.Value );

    public static implicit operator Either<TLeft, TRight>( in Either.RightValue<TRight> right )
        => new Right( right.right );

    public static implicit operator Either<TLeft, TRight>( TLeft  value ) => new Left( value );
    public static implicit operator Either<TLeft, TRight>( TRight right ) => new Right( right );
}