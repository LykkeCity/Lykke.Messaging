using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using Inceptum.Core.Messaging;
using Inceptum.DataBus.Messaging;
using NUnit.Framework;
using Rhino.Mocks;

namespace Inceptum.DataBus.Tests.Messaging
{
    [TestFixture]
    public class MessagingFeedWithInitializationBaseTests
    {
        public abstract class MessagingFeedWithInitializationMock<TData, TContext, TInitRequest, TInitResponse> : MessagingFeedWithInitializationBase<TData, TContext, TInitRequest, TInitResponse>
        {
           

            protected MessagingFeedWithInitializationMock(IMessagingEngine messagingEngine)
                : base(messagingEngine)
            {
            }

            public abstract DataBusMessagingEndpoint GetSubscriptionDataImpl(TContext context);
            public abstract TData ExtractInitialDataImpl(TInitResponse response, TContext context);
            public abstract IDisposable InitializeFeedImpl(Subject<TData> dataFeed, TInitResponse response, TContext context);
            public abstract TInitRequest GetInitRequestImpl(TContext context);

            protected override DataBusMessagingEndpoint GetEndpoint(TContext context)
            {
                return GetSubscriptionDataImpl(context);
            }

            protected override IEnumerable<TData> ExtractInitialData(TInitResponse response, TContext context)
            {
                return new[] { ExtractInitialDataImpl(response, context) };
            }

            protected override  TInitRequest GetInitRequest(TContext context)
            {
                return GetInitRequestImpl(context);
            }

            protected override IDisposable InitializeFeed(Subject<TData> dataFeed, TInitResponse response, TContext context)
            {
                return InitializeFeedImpl(dataFeed, response, context);
            }
        }



        [Test(Description = "Test of internal logic - observable should be deffered with notfication on subscription finished.")]
        public void SubscribtionFinishedCallbackTest()
        {
            var initCommand = new object(); 
            var transportEngine = MockRepository.GenerateMock<IMessagingEngine>();
            var dummyResponse = new object();

            var initSubscribtionCallback = new ValueWaiter<MulticastDelegate>();
            transportEngine.Expect(e => e.SendRequestAsync<object,object>(null, new Endpoint(), o => { }, exception =>{ })).IgnoreArguments()
                .WhenCalled(
                    invocation => initSubscribtionCallback.SetValue((MulticastDelegate)(invocation.Arguments[2]))
                    );

            transportEngine.Expect(e => e.Subscribe<int>(new Endpoint(), m => { })).IgnoreArguments().Return(Disposable.Empty).Repeat.Once();

            var feedProvider = MockRepository.GeneratePartialMock<MessagingFeedWithInitializationMock<int, string, object, object>>(transportEngine);
            feedProvider.Expect(p => p.GetSubscriptionDataImpl("feedContext")).Return(new Endpoint("dummyTransportId", "dummySubj"));
            feedProvider.Expect(p => p.GetInitRequestImpl("feedContext")).Return(initCommand);
            feedProvider.Expect(p => p.InitializeFeedImpl(null, null, null)).IgnoreArguments().Return(null);
            feedProvider.Expect(p => p.ExtractInitialDataImpl(dummyResponse, "feedContext")).Return(777);

            var observable = feedProvider.CreateFeed("feedContext") as DeferredObservable<int>;
            Assert.That(observable, Is.Not.Null, "Created observable id not deffered");

            var subscribedCallbackExecuted = new ValueWaiter<bool>();
            using (observable.Subscribe(Observer.Create<int>(i => { }), () => subscribedCallbackExecuted.SetValue(true)))
            {
                initSubscribtionCallback.WaitForValue(1000);
                Assert.That(subscribedCallbackExecuted.WaitForValue(200), Is.False, "Notify subscribed callback was called before init command response recieved");
                initSubscribtionCallback.Value.DynamicInvoke(dummyResponse);
                Assert.That(subscribedCallbackExecuted.WaitForValue(1000), Is.True, "Notify subscribed callback was not called after init command response recieved");
            }

        }




        [Test(Description = "Test of subscription on Messaging feed")]
        public void SubscribtionTest()
        {
            var initCommand = new object();
            var transportEngine = MockRepository.GenerateMock<IMessagingEngine>();
            var dummyResponse = new object();

            transportEngine.Expect(e => e.SendRequestAsync<object, object>(null, new Endpoint(), o => { }, exception => { })).IgnoreArguments()
                .WhenCalled(
                    invocation =>
                    {
                        var initSubscriptionCallback = (MulticastDelegate)(invocation.Arguments[2]);
                        initSubscriptionCallback.DynamicInvoke(dummyResponse);
                    });
            transportEngine.Expect(e => e.Subscribe<int>(new Endpoint(), m => { })).IgnoreArguments().Return(Disposable.Empty).Repeat.Once();

            var feedProvider = MockRepository.GeneratePartialMock<MessagingFeedWithInitializationMock<int, string, object, object>>(transportEngine);
            feedProvider.Expect(p => p.GetSubscriptionDataImpl("feedContext")).Return(new Endpoint("dummyTransportId", "dummySubj"));
            feedProvider.Expect(p => p.GetInitRequestImpl("feedContext")).Return(initCommand);
            feedProvider.Expect(p => p.InitializeFeedImpl(null, null, null)).IgnoreArguments().Return(null);
            feedProvider.Expect(p => p.ExtractInitialDataImpl(dummyResponse, "feedContext")).Return(777);

            var data = new ValueWaiter<int>();
            using (feedProvider.CreateFeed("feedContext").Subscribe(data.SetValue))
            {
                Assert.IsTrue(data.WaitForValue(220), "Subscribed handler was not called for initial data");
                Assert.AreEqual(777, data.Value, "Subscribed handler was not called with wrong initial data");
            }

            feedProvider.AssertWasCalled(p => p.InitializeFeedImpl(null, null, null), o => o.IgnoreArguments());
        }

     
        [Test(Description = "Any exception during init procedure should be reported with IObserver.OnError()")]
        public void InitFailureTest()
        {
            var initCommand = new object();
            var transportEngine = MockRepository.GenerateMock<IMessagingEngine>();
            var exception = new Exception("Test Error");
            transportEngine.Expect(e => e.SendRequestAsync<object, object>(null, new Endpoint(), o => { }, ex => { })).IgnoreArguments().Throw(exception);
            var feedProvider = MockRepository.GeneratePartialMock<MessagingFeedWithInitializationMock<int, string, object, object>>(transportEngine);

            feedProvider.Expect(p => p.GetSubscriptionDataImpl("feedContext")).Return(new Endpoint("dummyTransportId", "dummySubj"));
            feedProvider.Expect(p => p.GetInitRequestImpl("feedContext")).Return(initCommand);


            var error = new ValueWaiter<Exception>();
            using (feedProvider.CreateFeed("feedContext").Subscribe(_ => { }, error.SetValue))
            {
                Assert.IsTrue(error.WaitForValue(220), "Error was not reported afeter no execption on transport level");
                Assert.AreEqual(exception, error.Value, "Wrong exception was generated");
            }
        } 
    }
}