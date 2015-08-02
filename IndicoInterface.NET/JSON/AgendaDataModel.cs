using System;
using System.Collections.Generic;

namespace IndicoInterface.NET.JSON
{
    public class AdditionalInfo
    {
    }

    public class JDate
    {
        public string date { get; set; }
        public string tz { get; set; }
        public string time { get; set; }
    }

    public class Creator
    {
        public string _type { get; set; }
        public string emailHash { get; set; }
        public string affiliation { get; set; }
        public string _fossil { get; set; }
        public string fullName { get; set; }
        public string id { get; set; }
    }

    public class ModificationDate
    {
        public string date { get; set; }
        public string tz { get; set; }
        public string time { get; set; }
    }

    public class Speaker
    {
        public string _type { get; set; }
        public string emailHash { get; set; }
        public string affiliation { get; set; }
        public string _fossil { get; set; }
        public string fullName { get; set; }
        public string id { get; set; }
    }

    public class Primaryauthor
    {
        public string _type { get; set; }
        public string emailHash { get; set; }
        public string affiliation { get; set; }
        public string _fossil { get; set; }
        public string fullName { get; set; }
        public string id { get; set; }
    }

    public class Note
    {
    }

    public class Attachment
    {
        public string _type { get; set; }
        public DateTime modified_dt { get; set; }
        public string description { get; set; }
        public string title { get; set; }
        public string download_url { get; set; }
        public string filename { get; set; }
        public string content_type { get; set; }
        public string type { get; set; }
        public int id { get; set; }
        public int size { get; set; }
    }

    public class Folder
    {
        public string _type { get; set; }
        public IList<Attachment> attachments { get; set; }
        public string title { get; set; }
        public bool default_folder { get; set; }
        public int id { get; set; }
        public string description { get; set; }
    }

    public class Resource
    {
        public string _type { get; set; }
        public string name { get; set; }
        public string url { get; set; }
        public string fileName { get; set; }
        public string _fossil { get; set; }
        public string id { get; set; }
        public bool _deprecated { get; set; }
    }

    public class Material
    {
        public string _type { get; set; }
        public string title { get; set; }
        public string _fossil { get; set; }
        public string id { get; set; }
        public IList<Resource> resources { get; set; }
        public bool _deprecated { get; set; }
    }

    public class Contribution
    {
        public JDate startDate { get; set; }
        public JDate endDate { get; set; }
        public object session { get; set; }
        public IList<object> keywords { get; set; }
        public string id { get; set; }
        public IList<Speaker> speakers { get; set; }
        public IList<Primaryauthor> primaryauthors { get; set; }
        public string title { get; set; }
        public Note note { get; set; }
        public object location { get; set; }
        public string _fossil { get; set; }
        public object type { get; set; }
        public IList<Folder> folders { get; set; }
        public string _type { get; set; }
        public string description { get; set; }
        public object track { get; set; }
        public IList<Material> material { get; set; }
        public IList<object> coauthors { get; set; }
        public IList<object> subContributions { get; set; }
        public object room { get; set; }
        public string url { get; set; }
        public string roomFullname { get; set; }
    }

    public class Visibility
    {
        public string id { get; set; }
        public string name { get; set; }
    }

    public class CreationDate
    {
        public string date { get; set; }
        public string tz { get; set; }
        public string time { get; set; }
    }

    public class Chair
    {
        public string _type { get; set; }
        public string emailHash { get; set; }
        public string affiliation { get; set; }
        public string _fossil { get; set; }
        public string fullName { get; set; }
        public int id { get; set; }
    }

    public class Result
    {
        public JDate startDate { get; set; }
        public JDate endDate { get; set; }
        public Creator creator { get; set; }
        public bool hasAnyProtection { get; set; }
        public string roomFullname { get; set; }
        public ModificationDate modificationDate { get; set; }
        public string timezone { get; set; }
        public IList<Contribution> contributions { get; set; }
        public string id { get; set; }
        public string category { get; set; }
        public string title { get; set; }
        public Note note { get; set; }
        public object location { get; set; }
        public string _fossil { get; set; }
        public string type { get; set; }
        public string categoryId { get; set; }
        public IList<Folder> folders { get; set; }
        public string _type { get; set; }
        public string description { get; set; }
        public string roomMapURL { get; set; }
        public IList<Material> material { get; set; }
        public Visibility visibility { get; set; }
        public object address { get; set; }
        public CreationDate creationDate { get; set; }
        public object room { get; set; }
        public IList<Chair> chairs { get; set; }
        public string url { get; set; }
    }

    public class IndicoGetMeetingInfoReturn
    {
        public int count { get; set; }
        public AdditionalInfo additionalInfo { get; set; }
        public string _type { get; set; }
        public string url { get; set; }
        public IList<Result> results { get; set; }
        public int ts { get; set; }
    }
}
