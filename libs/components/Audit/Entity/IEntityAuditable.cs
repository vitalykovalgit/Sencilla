namespace Sencilla.Component.Audit;

/// <summary>
/// Opt-in marker: an entity that carries this interface has its inserts, updates and deletes recorded
/// (who / what / when / why) by the Sencilla.Component.Audit change-log. Deliberately opt-in — high-churn
/// entities (e.g. editor items) must NOT be audited. Marker only; the recorder reads the entity's Id and
/// scalar columns by reflection.
/// </summary>
public interface IEntityAuditable : IBaseEntity
{
}
