IndicoInterface.NET
===================

Access Indico and parse it for ease of use

Usage
=====

Use the object AgendaInfo to point to a meeting. Use AgendaLoader to fetch the agenda from the Indico server
and parse it into a reasonable data structure. Will also make some assumptions about what format you are interested
in when talks come in multiple formats.

As many Indico sites have specalized security, AgendaLoader takes a simple interface to fetch Indico URL's.
Use this to add your own interface. For an unprotected web site simple WebRequest and WebResponse objects work
just fine.

Compatible with windows phone 8.1, Windows Store, and .NET 4.5. API includes Task returns (e.g. async).

Development
===========

Built with VS2013, no nuget package dependencies. Tests only access public agendas, or local ones that have been
downloaded.

1. Extract from github, open, and build. Make sure all tests pass.
2. For new agenda format parsing, make sure that you add a new XML data file to the test rather than just fetching
   accross the web (the latter takes too long during testing).
3. Create nuget package from the IndicoInterface.NET directory with the command
   "nuget pack .\IndicoInterface.NET.csproj -Prop Configuration=Release"
