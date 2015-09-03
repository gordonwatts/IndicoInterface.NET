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
   across the web (the latter takes too long during testing).
3. Create nuget package from the IndicoInterface.NET directory with the command
   "nuget pack .\IndicoInterface.NET.csproj -Prop Configuration=Release"

Version History
===============
These are versions released to nuget

* 1.5.1 Make sure APIKey/etc. are usable when requesting access to meetings!
* 1.5.0 Added support for JSON meetings extraction, which is all that CERN's indico will now deal with. Should
        be backwards compatible.
* 1.2.0   Added support for new indico style event URLs. This is a more restful interface to the agenda server.
        Old URL's should still work. There is a WhiteList, and anything on it will have its URL done in the new
		style. As of this version only indico.cern.ch is on it. If it tries the old URL and it fails, but the new
		style works, then the site will be added to the WhiteList. See WhiteList object for more.
		See: https://github.com/gordonwatts/IndicoInterface.NET/issues?q=milestone%3A%22Fallback+and+New+URL+format%22+is%3Aclosed
* 1.1.0   Added FromShortString/ToShortString to give a compact unique representation (e.g. for a db). Size is about
        30 or 40 characters.
		XML serialization works. Binary can't because this is a PCL library.
* 1.0.0	First nuget release. Basic functionality. Raw indico data as well as a "nice" form parsed out from it.

