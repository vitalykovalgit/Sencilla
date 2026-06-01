-- Applies all Geography reference data in FK-dependency order.
-- Ships in the package's seed/ folder; a consuming database includes it from its own
-- post-deployment script:  :r $(Sencilla_Component_Geography)/ApplyData.sql
-- $(Sencilla_Component_Geography) (this seed/ folder) is provided automatically by the
-- package's build/*.props.
:r $(Sencilla_Component_Geography)/CountryData.sql
:r $(Sencilla_Component_Geography)/LanguageData.sql
:r $(Sencilla_Component_Geography)/CountryLanguageData.sql
