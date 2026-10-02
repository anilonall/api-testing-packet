using ApiTestingPacket.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiTestingPacket.Repository.Configurations;

internal static class OwnedConfiguration
{
    public static void Apply<T>(EntityTypeBuilder<T> builder) where T : OwnedEntity
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.UserId);
        builder.Property(x => x.UpdatedAt).IsConcurrencyToken();
    }
}

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasIndex(x => x.Email).IsUnique();
        builder.Property(x => x.Email).HasMaxLength(320);
        builder.Property(x => x.DisplayName).HasMaxLength(100);
    }
}

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.ExpiresAt });
        builder.HasOne(x => x.User).WithMany(x => x.RefreshTokens).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class CollectionConfiguration : IEntityTypeConfiguration<Collection>
{
    public void Configure(EntityTypeBuilder<Collection> builder)
    {
        OwnedConfiguration.Apply(builder);
        builder.HasIndex(x => new { x.UserId, x.Name });
    }
}

public sealed class CollectionFolderConfiguration : IEntityTypeConfiguration<CollectionFolder>
{
    public void Configure(EntityTypeBuilder<CollectionFolder> builder)
    {
        OwnedConfiguration.Apply(builder);
        builder.HasOne(x => x.Collection).WithMany(x => x.Folders).HasForeignKey(x => x.CollectionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Parent).WithMany().HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ApiRequestConfiguration : IEntityTypeConfiguration<ApiRequest>
{
    public void Configure(EntityTypeBuilder<ApiRequest> builder)
    {
        OwnedConfiguration.Apply(builder);
        builder.HasOne(x => x.Collection).WithMany(x => x.Requests).HasForeignKey(x => x.CollectionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Folder).WithMany().HasForeignKey(x => x.FolderId).OnDelete(DeleteBehavior.SetNull);
        builder.Property(x => x.DefinitionJson).HasColumnType("jsonb");
        builder.HasIndex(x => new { x.UserId, x.CollectionId, x.SortOrder });
    }
}

public sealed class EnvironmentConfiguration : IEntityTypeConfiguration<ApiEnvironment>
{
    public void Configure(EntityTypeBuilder<ApiEnvironment> builder) { OwnedConfiguration.Apply(builder); builder.Property(x => x.VariablesJson).HasColumnType("jsonb"); }
}

public sealed class ScenarioConfiguration : IEntityTypeConfiguration<Scenario>
{
    public void Configure(EntityTypeBuilder<Scenario> builder) { OwnedConfiguration.Apply(builder); builder.Property(x => x.DefinitionJson).HasColumnType("jsonb"); }
}

public sealed class ExecutionConfiguration : IEntityTypeConfiguration<Execution>
{
    public void Configure(EntityTypeBuilder<Execution> builder)
    {
        OwnedConfiguration.Apply(builder);
        builder.Property(x => x.SnapshotJson).HasColumnType("jsonb");
        builder.HasIndex(x => new { x.UserId, x.CreatedAt });
    }
}
