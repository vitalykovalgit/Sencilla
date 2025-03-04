﻿namespace Sencilla.Component.I18n;

public class Resource : IEntity<string>
{
    public string Id { get; set; }

    public string Description { get; set; }

    [NotMapped]
    public DateTime CreatedDate { get; set; }

    [NotMapped]
    public DateTime UpdatedDate { get; set; }

    [InverseProperty(nameof(Translation.Resource))]
    public ICollection<Translation> Translations { get; set; }

    public string[] SearchBy => [nameof(Id), nameof(Description)];
}
