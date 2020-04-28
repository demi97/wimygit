
namespace WimyGit
{
    //add some annotaion just for test
	public interface ILogger
	{
		void AddLog(string msg);
		void AddLog(System.Collections.Generic.List<string> msgs);
	}
}
