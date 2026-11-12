using System.Runtime.CompilerServices;

// The importer is part of Cozy Nodes itself, not consumer-facing API. It needs
// access to the imported graph schema without making that schema public to
// every project that references the runtime assembly.
[assembly: InternalsVisibleTo("shaderfactory.cozynodes.editor")]
