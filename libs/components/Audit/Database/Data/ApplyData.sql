-- Applies all Audit reference data (the audit.* enum lookup tables).
-- Ships in the package's seed/ folder; a consuming database includes it from its own
-- post-deployment script:  :r $(Sencilla_Component_Audit)/ApplyData.sql
-- $(Sencilla_Component_Audit) (this seed/ folder) is provided automatically by the
-- package's build/*.props.
:r $(Sencilla_Component_Audit)/AuditActionData.sql
:r $(Sencilla_Component_Audit)/ActorTypeData.sql
