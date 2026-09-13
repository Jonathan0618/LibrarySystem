# Administrator and Secret Operations

## First production administrator

1. Put `IdentitySeed__AdministratorEmail`, a unique strong `IdentitySeed__AdministratorPassword`, and `IdentitySeed__EnableAdministratorBootstrap=true` in the deployment secret store, never in JSON or source control.
2. Start one application instance. Bootstrap refuses to create a second administrator and records the action.
3. Confirm the address, sign in, and immediately replace the bootstrap password. The account is forced through the password-change flow.
4. Remove all three bootstrap secret values, set the switch to false, restart, and confirm bootstrap cannot run again.
5. Create named administrator accounts for authorized operators; never share the bootstrap identity.

## Production secrets

Store the SQL connection string, SMTP username/password, bootstrap values, and any external monitoring credentials in the platform secret store with access limited to the application identity and approved operators. Rotate a secret after suspected disclosure or staff departure and verify notification, database, login, and recovery paths afterward. Persistent Data Protection keys are operationally sensitive: restrict their directory permissions, back them up with the application recovery set, and do not delete them during routine deployments.
