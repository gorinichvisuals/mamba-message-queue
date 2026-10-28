#!/bin/sh
set -eu

: "${MambaServerOptions__Authentication__Username:?Set MambaServerOptions__Authentication__Username to a non-empty value}"
: "${MambaServerOptions__Authentication__Password:?Set MambaServerOptions__Authentication__Password to a non-empty value}"
: "${MambaServerOptions__Authentication__ServerKey:?Set MambaServerOptions__Authentication__ServerKey to a non-empty value}"

exec dotnet MambaMQ.Server.dll "$@"