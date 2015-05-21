# MassTransit RavenDB Persistence
RavenDB persistence support for MassTransit

## Note
Currently only one saga type can be supported per IDocumnetStore instance. This is becuase a Json Converter is added to the list of converters in the document store json serializer that wraps around the saga instance. This is used so that it the state property deserialization can fetch the state from the saga instance. Therefore to have multiple saga instances, a unique IDocumentStore instance must be used for each saga repository.
