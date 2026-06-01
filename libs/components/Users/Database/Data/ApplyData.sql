-- Applies all Users reference data (independent sec.* lookup tables).
-- Ships in the package's seed/ folder; a consuming database includes it from its own
-- post-deployment script:  :r $(Sencilla_Component_Users)/ApplyData.sql
-- $(Sencilla_Component_Users) (this seed/ folder) is provided automatically by the
-- package's build/*.props.
:r $(Sencilla_Component_Users)/UserTypeData.sql
:r $(Sencilla_Component_Users)/UserStatusData.sql
:r $(Sencilla_Component_Users)/UserGenderData.sql
:r $(Sencilla_Component_Users)/UserContactTypeData.sql
:r $(Sencilla_Component_Users)/UserAddressTypeData.sql
