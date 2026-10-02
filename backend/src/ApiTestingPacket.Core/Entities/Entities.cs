namespace ApiTestingPacket.Core.Entities;

public abstract class OwnedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Email { get; set; }
    public required string DisplayName { get; set; }
    public required string PasswordHash { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<RefreshToken> RefreshTokens { get; set; } = [];
}

public sealed class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public required string TokenHash { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RevokedAt { get; set; }
    public Guid? ReplacedById { get; set; }
}

public sealed class Collection : OwnedEntity
{
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public List<CollectionFolder> Folders { get; set; } = [];
    public List<ApiRequest> Requests { get; set; } = [];
}

public sealed class CollectionFolder : OwnedEntity
{
    public Guid CollectionId { get; set; }
    public Collection Collection { get; set; } = null!;
    public Guid? ParentId { get; set; }
    public CollectionFolder? Parent { get; set; }
    public required string Name { get; set; }
    public int SortOrder { get; set; }
}

public sealed class ApiRequest : OwnedEntity
{
    public Guid CollectionId { get; set; }
    public Collection Collection { get; set; } = null!;
    public Guid? FolderId { get; set; }
    public CollectionFolder? Folder { get; set; }
    public required string Name { get; set; }
    public required string Method { get; set; }
    public required string Url { get; set; }
    public required string DefinitionJson { get; set; }
    public int SortOrder { get; set; }
}

public sealed class ApiEnvironment : OwnedEntity
{
    public required string Name { get; set; }
    public required string VariablesJson { get; set; }
}

public sealed class Scenario : OwnedEntity
{
    public required string Name { get; set; }
    public required string DefinitionJson { get; set; }
}

public sealed class Execution : OwnedEntity
{
    public Guid? RequestId { get; set; }
    public Guid? ScenarioId { get; set; }
    public required string RequestName { get; set; }
    public required string Method { get; set; }
    public required string SafeUrl { get; set; }
    public int? StatusCode { get; set; }
    public long DurationMs { get; set; }
    public bool Passed { get; set; }
    public required string SnapshotJson { get; set; }
}
