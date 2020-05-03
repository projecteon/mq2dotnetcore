namespace RhinoBot.ToonHelpers
{
	public class ToonAction
	{
		public string Id { get; set; }
		public ToonActionType ActionType { get; set; }
		public string ActionValue { get; set; }
		public int DelayAfterExecuting { get; set; }
		public int DelayBeforeExecuting { get; set; }
		public ToonIdentifierType IdentifierType { get; set; }
		public string IdentifierValue { get; set; }
	}
}
