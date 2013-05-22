// Type: Matrix.Framework.SuperPool.Clients.SuperPoolClient
// Assembly: Matrix.Framework.SuperPool.Standalone, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// Assembly location: C:\Users\christian\Documents\Matrix_SuperPool_Standalone\Tests\Matrix.Framework.SuperPool.Demonstration\bin\Debug\Matrix.Framework.SuperPool.Standalone.dll

using Matrix.Common.Core;
using Matrix.Common.Core.Collections;
using Matrix.Common.Core.Identification;
using Matrix.Framework.MessageBus.Clients;
using Matrix.Framework.MessageBus.Core;
using Matrix.Framework.SuperPool.Call;
using Matrix.Framework.SuperPool.Core;
using Matrix.Framework.SuperPool.DynamicProxy;
using Matrix.Framework.SuperPool.Subscription;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace Matrix.Framework.SuperPool.Clients
{
  public class SuperPoolClient : ActiveInvocatorClient, ISuperPoolClient
  {
    private object _syncRoot = new object();
    private HotSwapList<Type> _consumerInterfacesHotSwap = new HotSwapList<Type>();
    private Dictionary<long, SyncCallInfo> _syncCalls = new Dictionary<long, SyncCallInfo>();
    private volatile bool _autoControlInvoke = true;
    private TimeSpan _defaultSyncCallTimeout = TimeSpan.FromSeconds(3.0);
    public const int GarbageCollectorIntervalMs = 5000;
    private Timer _garbageCollectorTimer;
    private volatile SuperPool _superPool;
    private SuperPoolSourceUpdateDelegate SourceUpdatedEvent;
    private SuperPoolClientUpdateDelegate SuperPoolAssignedEvent;
    private SuperPoolClientUpdateDelegate SuperPoolReleasedEvent;

    public int PendingSyncCallsCount
    {
      get
      {
        return this._syncCalls.Count;
      }
    }

    public SuperPool SuperPool
    {
      get
      {
        return this._superPool;
      }
    }

    public bool AutoControlInvoke
    {
      get
      {
        return this._autoControlInvoke;
      }
      set
      {
        this._autoControlInvoke = value;
      }
    }

    public TimeSpan DefaultSyncCallTimeout
    {
      get
      {
        lock (this._syncRoot)
          return this._defaultSyncCallTimeout;
      }
      set
      {
        lock (this._syncRoot)
          this._defaultSyncCallTimeout = value;
      }
    }

    public Envelope.DuplicationModeEnum EnvelopeDuplicationMode { get; set; }

    public Envelope.DuplicationModeEnum EnvelopeMultiReceiverDuplicationMode { get; set; }

    public override object Source
    {
      set
      {
        if (this._superPool == null)
          ;
        if (this.Source == value)
          return;
        object source = this.Source;
        base.Source = value;
        if (value == null)
          this._consumerInterfacesHotSwap.Clear();
        else
          this._consumerInterfacesHotSwap.SetToRange(ReflectionHelper.GatherTypeAttributeMarkedInterfaces(value.GetType(), typeof (SuperPoolInterfaceAttribute)));
        SuperPoolSourceUpdateDelegate sourceUpdateDelegate = this.SourceUpdatedEvent;
        if (sourceUpdateDelegate == null)
          return;
        sourceUpdateDelegate((ISuperPoolClient) this, source, value);
      }
    }

    internal string Name
    {
      get
      {
        return this.Id.Name;
      }
    }

    public event SuperPoolSourceUpdateDelegate SourceUpdatedEvent
    {
      add
      {
        SuperPoolSourceUpdateDelegate sourceUpdateDelegate = this.SourceUpdatedEvent;
        SuperPoolSourceUpdateDelegate comparand;
        do
        {
          comparand = sourceUpdateDelegate;
          sourceUpdateDelegate = Interlocked.CompareExchange<SuperPoolSourceUpdateDelegate>(ref this.SourceUpdatedEvent, comparand + value, comparand);
        }
        while (sourceUpdateDelegate != comparand);
      }
      remove
      {
        SuperPoolSourceUpdateDelegate sourceUpdateDelegate = this.SourceUpdatedEvent;
        SuperPoolSourceUpdateDelegate comparand;
        do
        {
          comparand = sourceUpdateDelegate;
          sourceUpdateDelegate = Interlocked.CompareExchange<SuperPoolSourceUpdateDelegate>(ref this.SourceUpdatedEvent, comparand - value, comparand);
        }
        while (sourceUpdateDelegate != comparand);
      }
    }

    public event SuperPoolClientUpdateDelegate SuperPoolAssignedEvent
    {
      add
      {
        SuperPoolClientUpdateDelegate clientUpdateDelegate = this.SuperPoolAssignedEvent;
        SuperPoolClientUpdateDelegate comparand;
        do
        {
          comparand = clientUpdateDelegate;
          clientUpdateDelegate = Interlocked.CompareExchange<SuperPoolClientUpdateDelegate>(ref this.SuperPoolAssignedEvent, comparand + value, comparand);
        }
        while (clientUpdateDelegate != comparand);
      }
      remove
      {
        SuperPoolClientUpdateDelegate clientUpdateDelegate = this.SuperPoolAssignedEvent;
        SuperPoolClientUpdateDelegate comparand;
        do
        {
          comparand = clientUpdateDelegate;
          clientUpdateDelegate = Interlocked.CompareExchange<SuperPoolClientUpdateDelegate>(ref this.SuperPoolAssignedEvent, comparand - value, comparand);
        }
        while (clientUpdateDelegate != comparand);
      }
    }

    public event SuperPoolClientUpdateDelegate SuperPoolReleasedEvent
    {
      add
      {
        SuperPoolClientUpdateDelegate clientUpdateDelegate = this.SuperPoolReleasedEvent;
        SuperPoolClientUpdateDelegate comparand;
        do
        {
          comparand = clientUpdateDelegate;
          clientUpdateDelegate = Interlocked.CompareExchange<SuperPoolClientUpdateDelegate>(ref this.SuperPoolReleasedEvent, comparand + value, comparand);
        }
        while (clientUpdateDelegate != comparand);
      }
      remove
      {
        SuperPoolClientUpdateDelegate clientUpdateDelegate = this.SuperPoolReleasedEvent;
        SuperPoolClientUpdateDelegate comparand;
        do
        {
          comparand = clientUpdateDelegate;
          clientUpdateDelegate = Interlocked.CompareExchange<SuperPoolClientUpdateDelegate>(ref this.SuperPoolReleasedEvent, comparand - value, comparand);
        }
        while (clientUpdateDelegate != comparand);
      }
    }

    public SuperPoolClient(ClientId id, object source)
      : base(id)
    {
      this.EnvelopeDuplicationMode = Envelope.DuplicationModeEnum.None;
      this.EnvelopeMultiReceiverDuplicationMode = Envelope.DuplicationModeEnum.DuplicateBoth;
      if (source != null)
        this.Source = source;
      this._garbageCollectorTimer = new Timer(new TimerCallback(this.CollectGarbage), (object) null, 5000, 5000);
    }

    public SuperPoolClient(string name, object source)
      : this(new ClientId(name), source)
    {
    }

    public override void Dispose()
    {
      this._garbageCollectorTimer.Dispose();
      lock (this._syncCalls)
      {
        foreach (SyncCallInfo item_0 in this._syncCalls.Values)
          item_0.Dispose();
        this._syncCalls.Clear();
      }
      this.Source = (object) null;
      this.SourceUpdatedEvent = (SuperPoolSourceUpdateDelegate) null;
      base.Dispose();
    }

    private void CollectGarbage(object state)
    {
      lock (this._syncCalls)
      {
        List<long> local_0 = (List<long>) null;
        foreach (SyncCallInfo item_0 in this._syncCalls.Values)
        {
          if (item_0.IsMultiResponse && item_0.IsMultiResponseComplete)
          {
            if (local_0 == null)
              local_0 = new List<long>();
            local_0.Add(item_0.CallId);
            break;
          }
        }
        if (local_0 == null)
          return;
        foreach (long item_1 in local_0)
          this._syncCalls.Remove(item_1);
      }
    }

    internal void AssignSuperPool(SuperPool superPool)
    {
      lock (this._syncRoot)
      {
        if (this._superPool != null && this._superPool != superPool)
          throw new Exception("Client already assigned to another super pool.");
        this._superPool = superPool;
      }
      SuperPoolClientUpdateDelegate clientUpdateDelegate = this.SuperPoolAssignedEvent;
      if (clientUpdateDelegate == null)
        return;
      clientUpdateDelegate((ISuperPoolClient) this);
    }

    internal void ReleaseSuperPool()
    {
      lock (this._syncRoot)
        this._superPool = (SuperPool) null;
      SuperPoolClientUpdateDelegate clientUpdateDelegate = this.SuperPoolReleasedEvent;
      if (clientUpdateDelegate == null)
        return;
      clientUpdateDelegate((ISuperPoolClient) this);
    }

    protected override void OnPerformExecution(Envelope envelope)
    {
      object source = this.Source;
      if (source == null || envelope.Message.GetType() != typeof (SuperPoolCall))
      {
        base.OnPerformExecution(envelope);
      }
      else
      {
        try
        {
          SuperPoolCall call = envelope.Message as SuperPoolCall;
          object obj1;
          if (call.State == SuperPoolCall.StateEnum.Responding)
          {
            object result = call.Parameters.Length > 0 ? call.Parameters[0] : (object) null;
            Exception exception = call.Parameters.Length > 1 ? call.Parameters[1] as Exception : (Exception) null;
            long id = call.Id;
            SyncCallInfo syncCallInfo = (SyncCallInfo) null;
            lock (this._syncCalls)
            {
              if (this._syncCalls.TryGetValue(id, out syncCallInfo))
              {
                if (!syncCallInfo.IsMultiResponse)
                  this._syncCalls.Remove(id);
              }
              else
                syncCallInfo = (SyncCallInfo) null;
            }
            if (syncCallInfo != null)
            {
              syncCallInfo.AcceptResponse(this, result, exception);
              if (syncCallInfo.IsMultiResponse && syncCallInfo.IsMultiResponseComplete)
              {
                lock (this._syncCalls)
                  this._syncCalls.Remove(id);
              }
            }
          }
          else if (call.State == SuperPoolCall.StateEnum.Requesting)
          {
            if (this._consumerInterfacesHotSwap.Contains(call.MethodInfoLocal.ReflectedType))
            {
              obj1 = (object) null;
              Exception exception = (Exception) null;
              object obj2 = this.PerformCall(call, source, out exception);
              if (call.RequestResponse)
              {
                call.State = SuperPoolCall.StateEnum.Responding;
                if (exception == null)
                  call.Parameters = new object[1]
                  {
                    obj2
                  };
                else
                  call.Parameters = new object[2]
                  {
                    obj2,
                    (object) exception
                  };
                SuperPool superPool = this._superPool;
                if (superPool == null)
                  return;
                IMessageBus messageBus = superPool.MessageBus;
                if (messageBus == null)
                  return;
                int num = (int) messageBus.Respond(envelope, new Envelope((object) call)
                {
                  DuplicationMode = this.EnvelopeDuplicationMode
                });
              }
              else
                call.State = SuperPoolCall.StateEnum.Finished;
            }
            else if (call.MethodInfoLocal != null)
              ;
          }
          else if (call.State == SuperPoolCall.StateEnum.EventRaise)
          {
            Exception exception;
            obj1 = this.PerformCall(call, source, out exception);
            call.State = SuperPoolCall.StateEnum.Finished;
          }
        }
        catch (Exception ex)
        {
        }
      }
    }

    protected object PerformCall(SuperPoolCall call, object target, out Exception exception)
    {
      object obj = call.Call(target, this.AutoControlInvoke, out exception);
      if (exception == null)
        ;
      return obj;
    }

    internal object ProcessCall(SuperPoolProxyCall proxyCall)
    {
      SuperPoolInvocation superPoolInvocation = (SuperPoolInvocation) this._superPool;
      if (superPoolInvocation == null)
        return ProxyTypeManager.GetTypeDefaultValue(proxyCall.ReturnType);
      IMessageBus messageBus = superPoolInvocation.MessageBus;
      if (messageBus == null || proxyCall.Processed)
        return ProxyTypeManager.GetTypeDefaultValue(proxyCall.ReturnType);
      if (proxyCall.Mode == SuperPoolProxyCall.ModeEnum.DirectCall)
      {
        MessageBusClient localClientInstance = messageBus.GetLocalClientInstance(proxyCall.ReceiversIds[0]);
        if (localClientInstance == null || !(localClientInstance is SuperPoolClient))
          return ProxyTypeManager.GetTypeDefaultValue(proxyCall.ReturnType);
        else
          return FastInvokeHelper.GetMethodInvoker(proxyCall.MethodInfo.ProxyMethodInfo, true, true)(((ActiveInvocatorClient) localClientInstance).Source, proxyCall.Parameters);
      }
      else
      {
        if (proxyCall.Mode == SuperPoolProxyCall.ModeEnum.CallFirst)
        {
          ClientId clientId = this.Resolve(proxyCall.MethodInfo.ProxyMethodInfo.DeclaringType);
          if ((ComponentId) clientId == (ComponentId) null)
            return ProxyTypeManager.GetTypeDefaultValue(proxyCall.ReturnType);
          proxyCall.ReceiversIds = new List<ClientId>()
          {
            clientId
          };
        }
        SuperPoolCall superPoolCall = new SuperPoolCall(superPoolInvocation.GetUniqueCallId());
        superPoolCall.Parameters = proxyCall.Parameters;
        superPoolCall.MethodInfoLocal = proxyCall.MethodInfo.ProxyMethodInfo;
        superPoolCall.State = SuperPoolCall.StateEnum.Requesting;
        superPoolCall.RequestResponse = proxyCall.IsSynchronous || proxyCall.IsAsyncResultExpecting;
        proxyCall.Processed = true;
        foreach (ParameterInfo parameterInfo in superPoolCall.MethodInfoLocal.GetParameters())
        {
          if (parameterInfo.IsOut || parameterInfo.IsRetval || parameterInfo.IsOut || parameterInfo.ParameterType.IsByRef)
            throw new NotImplementedException("Super pool calls do not support optional, out and ref parameters");
        }
        SyncCallInfo syncCallInfo = (SyncCallInfo) null;
        if (proxyCall.IsSynchronous || proxyCall.IsAsyncResultExpecting)
        {
          syncCallInfo = new SyncCallInfo(superPoolCall.Id)
          {
            AsyncResultState = proxyCall.AsyncResultState,
            AsyncResultDelegate = proxyCall.AsyncResultDelegate,
            AsyncResultTimeout = proxyCall.AsyncResultTimeout
          };
          lock (this._syncCalls)
            this._syncCalls[superPoolCall.Id] = syncCallInfo;
        }
        List<ClientId> list;
        if (proxyCall.ReceiversIds == null)
        {
          if (proxyCall.MethodInfo == null || proxyCall.MethodInfo.ProxyOwnerType == null)
            return ProxyTypeManager.GetTypeDefaultValue(proxyCall.ReturnType);
          Type baseInterfaceType = proxyCall.MethodInfo.GetBaseInterfaceType();
          if (baseInterfaceType == null)
            return ProxyTypeManager.GetTypeDefaultValue(proxyCall.ReturnType);
          list = new List<ClientId>();
          foreach (ComponentId componentId in superPoolInvocation.GetInterfaceImplementors(baseInterfaceType))
          {
            if (componentId != (ComponentId) proxyCall.Sender.Id)
              list.Add((ClientId) componentId);
            if (proxyCall.IsSynchronous && list.Count > 0)
              break;
          }
        }
        else
          list = proxyCall.ReceiversIds;
        if (list.Count <= 0)
          return ProxyTypeManager.GetTypeDefaultValue(proxyCall.ReturnType);
        if (syncCallInfo != null && proxyCall.IsSynchronous)
          syncCallInfo.Event = new ManualResetEvent(false);
        Outcomes outcomes = messageBus.Send(this.Id, (IEnumerable<ClientId>) list, new Envelope((object) superPoolCall)
        {
          DuplicationMode = this.EnvelopeDuplicationMode
        }, proxyCall.RequestConfirmTimeout, 0 != 0);
        if (proxyCall.Outcome != null)
          proxyCall.Outcome.Result = outcomes;
        if (outcomes != Outcomes.Success || (syncCallInfo == null || !proxyCall.IsSynchronous || !syncCallInfo.Event.WaitOne(proxyCall.Timeout.Value)))
          return ProxyTypeManager.GetTypeDefaultValue(proxyCall.ReturnType);
        else
          return syncCallInfo.Response;
      }
    }

    public bool RegisterConsumerInterface(Type interfaceType, bool verify)
    {
      if (verify)
      {
        object source = this.Source;
        if (source == null || !new List<Type>((IEnumerable<Type>) source.GetType().GetInterfaces()).Contains(interfaceType))
          return false;
      }
      return this._consumerInterfacesHotSwap.AddUnique(interfaceType);
    }

    protected TType DoCall<TType>(ComponentId receiverId, TimeSpan? requestConfirmTimeout, TimeSpan? timeout, AsyncCallResultDelegate asyncResultDelegate, object asyncResultState, TimeSpan? asyncResultTimeout, SuperPoolProxyCall.ModeEnum callMode, CallOutcome outcome) where TType : class
    {
      SuperPool superPool = this._superPool;
      if (superPool == null)
        return default (TType);
      TType result;
      SuperPoolProxyCall call;
      if (!superPool.Call<TType>(this, receiverId, out result, out call))
        return default (TType);
      call.AsyncResultDelegate = asyncResultDelegate;
      call.AsyncResultState = asyncResultState;
      call.AsyncResultTimeout = asyncResultTimeout;
      call.RequestConfirmTimeout = requestConfirmTimeout;
      call.Timeout = timeout;
      call.Mode = callMode;
      call.Outcome = outcome;
      return result;
    }

    protected TType DoCallMany<TType>(IEnumerable<ComponentId> receivers, TimeSpan? timeOut) where TType : class
    {
      SuperPool superPool = this._superPool;
      if (superPool == null)
        return default (TType);
      TType result;
      SuperPoolProxyCall call;
      if (!superPool.Call<TType>(this, receivers, out result, out call))
        return default (TType);
      call.Timeout = timeOut;
      return result;
    }

    public TType CallSyncFirst<TType>(TimeSpan timeOut) where TType : class
    {
      return this.DoCall<TType>((ComponentId) null, new TimeSpan?(), new TimeSpan?(timeOut), (AsyncCallResultDelegate) null, (object) null, new TimeSpan?(), SuperPoolProxyCall.ModeEnum.Default, (CallOutcome) null);
    }

    public TType CallSync<TType>(ComponentId receiverId) where TType : class
    {
      return this.DoCall<TType>(receiverId, new TimeSpan?(), new TimeSpan?(this.DefaultSyncCallTimeout), (AsyncCallResultDelegate) null, (object) null, new TimeSpan?(), SuperPoolProxyCall.ModeEnum.Default, (CallOutcome) null);
    }

    public TType CallSync<TType>(ComponentId receiverId, TimeSpan timeOut) where TType : class
    {
      return this.DoCall<TType>(receiverId, new TimeSpan?(), new TimeSpan?(timeOut), (AsyncCallResultDelegate) null, (object) null, new TimeSpan?(), SuperPoolProxyCall.ModeEnum.Default, (CallOutcome) null);
    }

    public TType CallConfirmed<TType>(ComponentId receiverId, TimeSpan? confirmTimeout, out CallOutcome outcome) where TType : class
    {
      outcome = new CallOutcome();
      return this.DoCall<TType>(receiverId, confirmTimeout, new TimeSpan?(), (AsyncCallResultDelegate) null, (object) null, new TimeSpan?(), SuperPoolProxyCall.ModeEnum.Default, outcome);
    }

    public TType Call<TType>(ComponentId receiverId, AsyncCallResultDelegate asyncDelegate, object state) where TType : class
    {
      return this.DoCall<TType>(receiverId, new TimeSpan?(), new TimeSpan?(), asyncDelegate, state, new TimeSpan?(), SuperPoolProxyCall.ModeEnum.Default, (CallOutcome) null);
    }

    public TType Call<TType>(ComponentId receiverId, AsyncCallResultDelegate asyncDelegate) where TType : class
    {
      return this.DoCall<TType>((ComponentId) null, new TimeSpan?(), new TimeSpan?(), asyncDelegate, (object) null, new TimeSpan?(), SuperPoolProxyCall.ModeEnum.Default, (CallOutcome) null);
    }

    public TType CallAll<TType>(AsyncCallResultDelegate asyncDelegate, TimeSpan asyncResultTimeout) where TType : class
    {
      return this.DoCall<TType>((ComponentId) null, new TimeSpan?(), new TimeSpan?(), asyncDelegate, (object) null, new TimeSpan?(asyncResultTimeout), SuperPoolProxyCall.ModeEnum.Default, (CallOutcome) null);
    }

    public TType CallAll<TType>(AsyncCallResultDelegate asyncDelegate, object state, TimeSpan asyncResultTimeout) where TType : class
    {
      return this.DoCall<TType>((ComponentId) null, new TimeSpan?(), new TimeSpan?(), asyncDelegate, state, new TimeSpan?(asyncResultTimeout), SuperPoolProxyCall.ModeEnum.Default, (CallOutcome) null);
    }

    public TType CallAll<TType>() where TType : class
    {
      return this.DoCall<TType>((ComponentId) null, new TimeSpan?(), new TimeSpan?(), (AsyncCallResultDelegate) null, (object) null, new TimeSpan?(), SuperPoolProxyCall.ModeEnum.Default, (CallOutcome) null);
    }

    public TType CallFirst<TType>() where TType : class
    {
      return this.DoCall<TType>((ComponentId) null, new TimeSpan?(), new TimeSpan?(), (AsyncCallResultDelegate) null, (object) null, new TimeSpan?(), SuperPoolProxyCall.ModeEnum.CallFirst, (CallOutcome) null);
    }

    public TType Call<TType>(ComponentId receiverId) where TType : class
    {
      return this.DoCall<TType>(receiverId, new TimeSpan?(), new TimeSpan?(), (AsyncCallResultDelegate) null, (object) null, new TimeSpan?(), SuperPoolProxyCall.ModeEnum.Default, (CallOutcome) null);
    }

    public TType Call<TType>(IEnumerable<ComponentId> receivers) where TType : class
    {
      return this.DoCallMany<TType>(receivers, new TimeSpan?());
    }

    public TType CallDirectLocal<TType>(ComponentId receiver) where TType : class
    {
      return this.DoCall<TType>(receiver, new TimeSpan?(), new TimeSpan?(), (AsyncCallResultDelegate) null, (object) null, new TimeSpan?(), SuperPoolProxyCall.ModeEnum.DirectCall, (CallOutcome) null);
    }

    public TType SubscribeAll<TType>() where TType : class
    {
      return this.Subscribe<TType>(new EventSubscriptionRequest());
    }

    public TType Subscribe<TType>(ComponentId sourceId) where TType : class
    {
      return this.Subscribe<TType>(new EventSubscriptionRequest((ClientId) sourceId));
    }

    public TType Subscribe<TType>(EventSubscriptionRequest subscription) where TType : class
    {
      SuperPool superPool = this._superPool;
      if (superPool == null)
        return default (TType);
      TType resultValue;
      if (superPool.Subscribe<TType>(this, subscription, out resultValue))
        return resultValue;
      else
        return default (TType);
    }

    public ClientId Resolve<TInterfaceType>()
    {
      SuperPool superPool = this._superPool;
      if (superPool != null)
      {
        using (IEnumerator<ClientId> enumerator = superPool.GetInterfaceImplementors(typeof (TInterfaceType)).GetEnumerator())
        {
          if (enumerator.MoveNext())
            return enumerator.Current;
        }
      }
      return (ClientId) null;
    }

    public List<ClientId> ResolveAll<TInterfaceType>()
    {
      List<ClientId> list = new List<ClientId>();
      SuperPool superPool = this._superPool;
      if (superPool != null)
        list.AddRange(superPool.GetInterfaceImplementors(typeof (TInterfaceType)));
      return list;
    }

    public ClientId Resolve(Type interfaceType)
    {
      SuperPool superPool = this._superPool;
      if (superPool != null)
      {
        using (IEnumerator<ClientId> enumerator = superPool.GetInterfaceImplementors(interfaceType).GetEnumerator())
        {
          if (enumerator.MoveNext())
            return enumerator.Current;
        }
      }
      return (ClientId) null;
    }

    public List<ClientId> ResolveAll(Type interfaceType)
    {
      List<ClientId> list = new List<ClientId>();
      SuperPool superPool = this._superPool;
      if (superPool != null)
        list.AddRange(superPool.GetInterfaceImplementors(interfaceType));
      return list;
    }
  }
}
