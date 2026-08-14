namespace Sencilla.Component.Files.AmazonS3.Tests;

/// <summary>
/// Configuration-only registration: several S3 buckets side by side, each addressable by the
/// byte that <c>File.Storage</c> records, so adding a bucket never means changing code.
/// </summary>
public class AmazonS3RegistrationTests
{
    private static IConfiguration Config(Dictionary<string, string?> values)
        => new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static ServiceProvider Build(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddSencillaFiles(o => o.AddAmazonS3(configuration));
        return services.BuildServiceProvider();
    }

    [Fact]
    public void TwoS3Instances_AreRegisteredUnderTheirOwnIds_AndKeepTheirOwnOptions()
    {
        var provider = Build(Config(new Dictionary<string, string?>
        {
            ["SencillaFiles:Storages:hetzner:Provider"] = "AmazonS3",
            ["SencillaFiles:Storages:hetzner:Type"] = "3",
            ["SencillaFiles:Storages:hetzner:Bucket"] = "photoboost-qa",
            ["SencillaFiles:Storages:hetzner:ServiceUrl"] = "https://fsn1.your-objectstorage.com",
            ["SencillaFiles:Storages:hetzner:AuthenticationRegion"] = "fsn1",

            ["SencillaFiles:Storages:aws:Provider"] = "AmazonS3",
            ["SencillaFiles:Storages:aws:Type"] = "4",
            ["SencillaFiles:Storages:aws:Bucket"] = "photoboost-eu",
            ["SencillaFiles:Storages:aws:Region"] = "eu-central-1",
        }));

        var hetzner = provider.GetRequiredKeyedService<IFileStorage>((byte)3);
        var aws = provider.GetRequiredKeyedService<IFileStorage>((byte)4);

        Assert.Equal(3, hetzner.Type);
        Assert.Equal(4, aws.Type);
        Assert.NotSame(hetzner, aws);
    }

    [Fact]
    public void NamedInstance_IsNotOverwrittenByTheFlatSection()
    {
        // Both shapes present: the flat section must not rebind over the named instance, or the
        // two registrations collapse onto one bucket.
        var provider = Build(Config(new Dictionary<string, string?>
        {
            ["SencillaFiles:AmazonS3:Bucket"] = "legacy-bucket",
            ["SencillaFiles:AmazonS3:Region"] = "eu-central-1",

            ["SencillaFiles:Storages:hetzner:Provider"] = "AmazonS3",
            ["SencillaFiles:Storages:hetzner:Type"] = "4",
            ["SencillaFiles:Storages:hetzner:Bucket"] = "named-bucket",
            ["SencillaFiles:Storages:hetzner:ServiceUrl"] = "https://fsn1.your-objectstorage.com",
        }));

        Assert.Equal(3, provider.GetRequiredKeyedService<IFileStorage>((byte)3).Type);
        Assert.Equal(4, provider.GetRequiredKeyedService<IFileStorage>((byte)4).Type);
    }

    [Fact]
    public void FlatSection_StillRegistersUnderTheHistoricalIdThree()
    {
        var provider = Build(Config(new Dictionary<string, string?>
        {
            ["SencillaFiles:AmazonS3:Bucket"] = "photoboost",
            ["SencillaFiles:AmazonS3:Region"] = "eu-central-1",
        }));

        Assert.Equal(3, provider.GetRequiredKeyedService<IFileStorage>((byte)3).Type);
    }

    [Fact]
    public void UseAsDefault_MakesThatInstanceTheUnkeyedStorage()
    {
        var provider = Build(Config(new Dictionary<string, string?>
        {
            ["SencillaFiles:Storages:hetzner:Provider"] = "AmazonS3",
            ["SencillaFiles:Storages:hetzner:Type"] = "5",
            ["SencillaFiles:Storages:hetzner:UseAsDefault"] = "true",
            ["SencillaFiles:Storages:hetzner:Bucket"] = "photoboost-qa",
            ["SencillaFiles:Storages:hetzner:ServiceUrl"] = "https://fsn1.your-objectstorage.com",
        }));

        Assert.Equal(5, provider.GetRequiredService<IFileStorage>().Type);
    }

    [Fact]
    public void TwoInstancesSharingAnId_FailLoudly_RatherThanOneVanishing()
    {
        // The id is persisted in File.Storage. A silently dropped registration means files written
        // under that id come back from the wrong bucket.
        var configuration = Config(new Dictionary<string, string?>
        {
            ["SencillaFiles:Storages:one:Provider"] = "AmazonS3",
            ["SencillaFiles:Storages:one:Bucket"] = "bucket-one",
            ["SencillaFiles:Storages:one:Region"] = "eu-central-1",

            ["SencillaFiles:Storages:two:Provider"] = "AmazonS3",
            ["SencillaFiles:Storages:two:Bucket"] = "bucket-two",
            ["SencillaFiles:Storages:two:Region"] = "eu-central-1",
        });

        var ex = Assert.Throws<InvalidOperationException>(() => Build(configuration));
        Assert.Contains("already registered", ex.Message);
    }

    [Fact]
    public void NoS3Configuration_RegistersNothing()
    {
        var provider = Build(Config(new Dictionary<string, string?>
        {
            ["SencillaFiles:LocalDrive:RootPath"] = "/tmp/files",
        }));

        Assert.Null(provider.GetKeyedService<IFileStorage>((byte)3));
    }

    [Fact]
    public void MissingBucket_FailsAtStartup_NotAtTheFirstUpload()
    {
        var provider = Build(Config(new Dictionary<string, string?>
        {
            ["SencillaFiles:AmazonS3:Region"] = "eu-central-1",
        }));

        var ex = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredKeyedService<IFileStorage>((byte)3));
        Assert.Contains("Bucket", ex.Message);
    }

    [Fact]
    public void HetznerLocationInTheRegionField_IsRejectedWithTheFix()
    {
        // RegionEndpoint.GetBySystemName invents an AWS-partition region for "fsn1" and the SDK
        // then talks to <bucket>.s3.fsn1.amazonaws.com, which does not resolve. Catch it here.
        var provider = Build(Config(new Dictionary<string, string?>
        {
            ["SencillaFiles:AmazonS3:Bucket"] = "photoboost",
            ["SencillaFiles:AmazonS3:Region"] = "fsn1",
        }));

        var ex = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredKeyedService<IFileStorage>((byte)3));
        Assert.Contains(nameof(AmazonS3StorageOptions.ServiceUrl), ex.Message);
    }
}
