using System.Collections.Generic;

namespace MopsBot.Data.Tracker.APIResults.Wolfram
{
    public class Img
    {
        public string src { get; set; }
        public string alt { get; set; }
        public string title { get; set; }
        public int width { get; set; }
        public int height { get; set; }
    }

    public class Subpod
    {
        public string title { get; set; }
        public Img img { get; set; }
        public string plaintext { get; set; }
    }

    public class State
    {
        public string name { get; set; }
        public string input { get; set; }
    }

    public class Pod
    {
        public string title { get; set; }
        public string scanner { get; set; }
        public string id { get; set; }
        public int position { get; set; }
        public bool error { get; set; }
        public int numsubpods { get; set; }
        public bool primary { get; set; }
        public List<Subpod> subpods { get; set; }
        public List<State> states { get; set; }
    }

    public class Queryresult
    {
        public bool success { get; set; }
        public bool error { get; set; }
        public int numpods { get; set; }
        public string datatypes { get; set; }
        public string timedout { get; set; }
        public string timedoutpods { get; set; }
        public double timing { get; set; }
        public double parsetiming { get; set; }
        public bool parsetimedout { get; set; }
        public string recalculate { get; set; }
        public string id { get; set; }
        public string host { get; set; }
        public string server { get; set; }
        public string related { get; set; }
        public string version { get; set; }
        public List<Pod> pods { get; set; }
    }

    public class WolframResult
    {
        public Queryresult queryresult { get; set; }
    }
}