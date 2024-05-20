using UnionGen;
using UnionGen.Types;

namespace JwtDemo.Core.Users;

[Union<User, NotFound>]
public readonly partial struct GetUserResult;
