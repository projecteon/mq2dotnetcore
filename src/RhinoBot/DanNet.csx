//using System.Reflection;
//using System.Threading;
//using System.Threading.Tasks;
//using MQ2DotNet.MQ2API;
//using MQ2DotNet.MQ2API.DataTypes;
//using MQ2DotNet.Services;

//[MQ2Type("DanObservation")]
//public class DanObservationType : MQ2DataType
//{
//    public DanObservationType(MQ2TypeFactory typeFactory, MQ2TypeVar typeVar) : base(typeFactory, typeVar)
//    {
//    }

//    public long? Received => GetMember<Int64Type>("Received");

//    // Not actually a member, but there to make it obvious that ToString is how you get the value
//    public string Value => ToString();
//}

//[MQ2Type("DanNet")]
//public class DanNetType : MQ2DataType
//{
//    public DanNetType(MQ2TypeFactory typeFactory, MQ2TypeVar typeVar) : base(typeFactory, typeVar)
//    {
//        PeerCount = new IndexedMember<IntType, string, IntType, int>(this, "PeerCount");
//        Peers = new IndexedMember<StringType, string, StringType, int>(this, "Peers");
//        GroupCount = new IndexedMember<IntType, string, IntType, int>(this, "GroupCount");
//        Groups = new IndexedMember<StringType, string, StringType, int>(this, "Groups");
//        JoinedCount = new IndexedMember<IntType, string, IntType, int>(this, "JoinedCount");
//        Joined = new IndexedMember<StringType, string, StringType, int>(this, "Joined");
//        Observe = new IndexedMember<MQ2DataType, string>(this, "Observe");
//        ObserveSet = new IndexedMember<BoolType>(this, "ObserveSet");
//        ObserveReceived = new IndexedMember<Int64Type>(this, "ObserveReceived");
//    }

//    public string? Name => GetMember<StringType>("Name");
//    public string? Version => GetMember<StringType>("Version");
//    public bool Debug => GetMember<BoolType>("Debug");
//    public bool LocalEcho => GetMember<BoolType>("LocalEcho");
//    public bool CommandEcho => GetMember<BoolType>("CommandEcho");
//    public bool FullNames => GetMember<BoolType>("FullNames");
//    public bool FrontDelim => GetMember<BoolType>("FrontDelim");
//    public string? Timeout => GetMember<StringType>("Timeout");
//    public int? ObserveDelay => GetMember<IntType>("ObserveDelay");
//    public int? Evasive => GetMember<IntType>("Evasive");
//    public int? Expired => GetMember<IntType>("Expired");
//    public int? Keepalive => GetMember<IntType>("Keepalive");
//    public IndexedMember<IntType, string, IntType, int> PeerCount { get; } // These are all helper classes that let you access with []
//    public IndexedMember<StringType, string, StringType, int> Peers { get; }
//    public IndexedMember<IntType, string, IntType, int> GroupCount { get; }
//    public IndexedMember<StringType, string, StringType, int> Groups { get; }
//    public IndexedMember<IntType, string, IntType, int> JoinedCount { get; }
//    public IndexedMember<StringType, string, StringType, int> Joined { get; }
//    public DanObservationType? Query => GetMember<DanObservationType>("Query");
//    public long? QueryReceived => GetMember<Int64Type>("QueryReceived");
//    public IndexedMember<MQ2DataType, string> Observe { get; } // Could be DanObservationType or StringType :(
//    public int? ObserveCount => GetMember<IntType>("ObserveCount");
//    public IndexedMember<BoolType> ObserveSet { get; }
//    public IndexedMember<Int64Type> ObserveReceived { get; }
//}

//public interface IDanNetObserver : IDisposable
//{
//    Task<bool> WaitUntilAvailable(int timeoutMs, CancellationToken token);
//    string Character { get; }
//    string Query { get; }
//    string Value { get; }
//    bool Available { get; }
//    long? Received { get; }
//}

//public class DanNet
//{
//    private readonly MQ2 _mq2;
//    private readonly TLO _tlo;

//    public DanNet(MQ2 mq2, TLO tlo)
//    {
//        _mq2 = mq2;
//        _tlo = tlo;
//    }

//    public IDanNetObserver Observe(string character, string query)
//    {
//        return new DanNetObserver(_mq2, _tlo, character, query);
//    }

//    public IDanNetObserver Observe<TResult>(string character, Expression<Func<TLO, TResult>> expression) =>
//        Observe(character, Parse(expression));

//    public async Task<string> Query(string character, string query, int timeoutMs)
//    {
//        var command = $"/noparse /dquery {character} -q {query}";
//        _mq2.DoCommand(command);

//        var timeout = DateTimeOffset.Now + TimeSpan.FromMilliseconds(timeoutMs);
//        while (true)
//        {
//            if (_tlo.GetTLO<DanNetType>("DanNet").QueryReceived > 0)
//                return _tlo.GetTLO<DanNetType>("DanNet").Query.Value;
//            if (DateTimeOffset.Now > timeout)
//                return "";
//            await Task.Yield();
//        }
//    }

//    public Task<string> Query<TResult>(string character, Expression<Func<TLO, TResult>> expression, int timeoutMs) =>
//        Query(character, Parse(expression), timeoutMs);

//    private class DanNetObserver : IDanNetObserver
//    {
//        private readonly MQ2 _mq2;
//        private readonly TLO _tlo;

//        public DanNetObserver(MQ2 mq2, TLO tlo, string character, string query)
//        {
//            _mq2 = mq2;
//            _tlo = tlo;
//            Character = character;
//            Query = query;
            
//            _mq2.DoCommand($"/noparse /dobserve {Character} -q {Query}");
//        }

//        public async Task<bool> WaitUntilAvailable(int timeoutMs, CancellationToken token)
//        {
//            var endTime = DateTimeOffset.Now + TimeSpan.FromMilliseconds(timeoutMs);
//            while (true)
//            {
//                if (Received > 0)
//                    return true;
//                if (DateTimeOffset.Now > endTime)
//                    return false;
//                token.ThrowIfCancellationRequested();
//                await Task.Yield();
//            }
//        }

//        public string Character { get; }
//        public string Query { get; }
//        public bool Available => Received > 0;
//        public string Value => _tlo.GetTLO<DanNetType>("DanNet", Character).Observe[Query]?.ToString(); // May be null if not yet received
//        public long? Received => _tlo.GetTLO<DanNetType>("DanNet", Character).ObserveReceived[Query];

//        private void ReleaseUnmanagedResources()
//        {
//            _mq2.DoCommand($"/noparse /dobserve {Character} -q {Query} -drop");
//        }

//        public void Dispose()
//        {
//            ReleaseUnmanagedResources();
//            GC.SuppressFinalize(this);
//        }

//        ~DanNetObserver()
//        {
//            ReleaseUnmanagedResources();
//        }
//    }

//    private static string Parse<TResult>(Expression<Func<TLO, TResult>> expression)
//    {
//        if (expression is null)
//            throw new ArgumentNullException(nameof(expression));

//        var withBraces = ParseExpressionRoot(expression.Body);
//        return withBraces.Substring(2, withBraces.Length - 3); // Strip surrounding ${}
//    }

//    private static string ParseExpressionRoot(Expression expression)
//    {
//        if (expression is null)
//            throw new ArgumentNullException(nameof(expression));

//        var queryMembers = new List<string>();
//        do
//        {
//            switch (expression)
//            {
//                case ConstantExpression constant:
//                    return constant.Value.ToString();

//                case MemberExpression member:
//                    // If it's a closure, evaluate it and return it
//                    if (member.Expression is ConstantExpression closure && member.Member is FieldInfo field)
//                        return field.GetValue(closure.Value).ToString();

//                    // Otherwise, add the member to the list and go up the tree to the next one
//                    queryMembers.Add(member.Member.Name);
//                    expression = member.Expression;
//                    break;

//                case ParameterExpression _:
//                    // If we've reached the parameter, that's the TLO & root of this part of the tree, so take what we currently have and return it
//                    return "${" + string.Join(".", queryMembers.AsEnumerable().Reverse()) + "}";

//                case BinaryExpression binary:
//                    // Only support string adds (concatenation)
//                    if (binary.NodeType != ExpressionType.Add || binary.Left.Type != typeof(string) || binary.Right.Type != typeof(string))
//                        throw new NotSupportedException($"Unsupported binary operation in expression: {binary}");
//                    return string.Concat(ParseExpressionRoot(binary.Left), ParseExpressionRoot(binary.Right));

//                case MethodCallExpression method:
//                    // Index operation on IndexedTLO or IndexedMember
//                    if ((IsIndexedTLO(method.Method.DeclaringType) || IsIndexedMember(method.Method.DeclaringType)) && method.Method.Name == "get_Item")
//                    {
//                        if (!(method.Object is MemberExpression indexedMember))
//                            throw new NotSupportedException($"Unsupported method call in expression: {method}");

//                        var index = ParseExpressionRoot(method.Arguments.Single());
//                        queryMembers.Add($"{indexedMember.Member.Name}[{index}]");
//                        expression = indexedMember.Expression;
//                        break;
//                    }
//                    // String format (interpolation)
//                    else if (method.Method.DeclaringType == typeof(string) && method.Method.Name == "Format")
//                    {
//                        return string.Format(ParseExpressionRoot(method.Arguments[0]), method.Arguments.Skip(1).Select(ParseExpressionRoot).Cast<object>().ToArray());
//                    }

//                    throw new NotSupportedException($"Unsupported method call in expression: {method}");

//                default:
//                    throw new NotSupportedException($"Unsupported expression: {expression}");
//            }
//        } while (expression != null);

//        throw new NotSupportedException("Reached the end of the expression without managing to parse it");
//    }

//    private static bool IsIndexedTLO(Type type) => type.IsGenericType && _indexedTLOTypes.Contains(type.GetGenericTypeDefinition());

//    private static readonly Type[] _indexedTLOTypes =
//    {
//        typeof(TLO.IndexedTLO<>), typeof(TLO.IndexedTLO<,>),
//        typeof(TLO.IndexedTLO<,,,>)
//    };

//    private static bool IsIndexedMember(Type type) => type.IsGenericType && _indexedMemberTypes.Contains(type.GetGenericTypeDefinition());

//    private static readonly Type[] _indexedMemberTypes =
//    {
//        typeof(MQ2DataType.IndexedMember<>), typeof(MQ2DataType.IndexedMember<,>),
//        typeof(MQ2DataType.IndexedMember<,,,>), typeof(MQ2DataType.IndexedStringMember<>),
//        typeof(MQ2DataType.IndexedStringMember<,,>)
//    };
//}
