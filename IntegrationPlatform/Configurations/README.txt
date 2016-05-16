Notes on creating a new configuration template:

1.	MigrationSources can have the optional attribute "EndpointSystemName" to limit the provider choices during configuration.  To find the corresponding provider EndpointSystemName, see ProviderCapabilityAttribute on an adapter.
2.	The FilterString attribute on a FilterItem can contain the keyword "&lt;SourceIdentifier&gt;" (<SourceIdentifier>), which will be replaced with the corresponding MigrationSource's SourceIdentifier when configured.
3.	To pass initial validation:
	a.	Add this to the <Providers> tag:
		<Provider ReferenceName="00000000-0000-0000-0000-000000000000" FriendlyName="Empty Provider" />
	b.	Add the following attribute to each MigrationSource:
		ProviderReferenceName="00000000-0000-0000-0000-000000000000"
	c.	Provide default values for the following MigrationSource attributes:
		FriendlyName, SourceIdentifier, ServerIdentifier, ServerUrl
4.	You may choose to provide a default provider for a MigrationSource.