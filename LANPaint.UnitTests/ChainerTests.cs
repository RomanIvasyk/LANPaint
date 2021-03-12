using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using LANPaint.Services.Broadcast;
using LANPaint.Services.Broadcast.Decorators;
using Moq;
using Xunit;

namespace LANPaint.UnitTests
{
    public class ChainerTest
    {
        private readonly Mock<IBroadcast> _broadcastImplMock;

        public ChainerTest()
        {
            _broadcastImplMock = new Mock<IBroadcast>();
            _broadcastImplMock.Setup(broadcast => broadcast.SendAsync(It.IsAny<byte[]>()))
                .ReturnsAsync((byte[] dataParam) => dataParam.Length);
        }

        [Fact]
        public void Ctor_PassNullBroadcastImpl()
        {
            Assert.Throws<ArgumentNullException>(() => new Chainer(null));
        }

        [Fact]
        public void Ctor_LowerThanAllowedSegmentLength()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new Chainer(_broadcastImplMock.Object, Chainer.MinSegmentLength - 1));
        }

        [Fact]
        public void Ctor_HigherThanAllowedSegmentLength()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new Chainer(_broadcastImplMock.Object, Chainer.MaxSegmentLength + 1));
        }

        [Fact]
        public void Ctor_ValidSegmentLength()
        {
            var chainer = new Chainer(_broadcastImplMock.Object,
                Chainer.MinSegmentLength + (Chainer.MaxSegmentLength - Chainer.MinSegmentLength) / 2);
        }

        [Fact]
        public void LocalEndPoint_CheckInnerCall()
        {
            var chainer = new Chainer(_broadcastImplMock.Object);
            var endPoint = chainer.LocalEndPoint;
            _broadcastImplMock.Verify(broadcast => broadcast.LocalEndPoint, Times.Once);
        }

        //Does it even make sense to check this?
        [Fact]
        public async void SendAsync_CheckReturnValue()
        {
            const int dataLength = 10000;
            var data = new byte[dataLength];
            var chainer = new Chainer(_broadcastImplMock.Object);

            var result = await chainer.SendAsync(data);

            //We know that Chainer wraps data into Package, so sent amount of bytes
            //definitely will be more than initial payload size.
            Assert.True(result > dataLength);
        }

        [Fact]
        public async void SendReceiveAsync_CheckSendingReceivingOfTheSameData()
        {
            var storage = new Stack<byte[]>();
            var broadcastImplMock = new Mock<IBroadcast>();
            broadcastImplMock.Setup(broadcast => broadcast.SendAsync(It.IsAny<byte[]>()))
                .Callback((byte[] dataParam) => storage.Push(dataParam))
                .ReturnsAsync((byte[] dataParam) => dataParam.Length);
            broadcastImplMock.Setup(broadcast => broadcast.ReceiveAsync(CancellationToken.None))
                .ReturnsAsync(() => storage.Pop());
            var broadcastImpl = broadcastImplMock.Object;
            var data = RandomizeByteSequence(24000);

            await broadcastImpl.SendAsync(data);
            var result = await broadcastImpl.ReceiveAsync(CancellationToken.None);

            Assert.True(result.SequenceEqual(data));

            byte[] RandomizeByteSequence(int length)
            {
                var random = new Random();
                var sequence = new byte[length];

                for (var i = 0; i < length; i++)
                {
                    sequence[i] = (byte) random.Next(256);
                }

                return sequence;
            }
        }

        [Theory]
        [InlineData(10000, 8192, 2)]
        [InlineData(10000, 10000, 1)]
        [InlineData(20100, 10000, 3)]
        [InlineData(0, 8192, 0)]
        public async void SendAsync_CheckNumberOfSendCalls(int dataLength, int maxSegmentLength,
            int expectedSendCallsNumber)
        {
            var data = new byte[dataLength];
            var chainer = new Chainer(_broadcastImplMock.Object, maxSegmentLength);
            await chainer.SendAsync(data);
            _broadcastImplMock.Verify(broadcast => broadcast.SendAsync(It.IsAny<byte[]>()), Times.Exactly(expectedSendCallsNumber));
        }

        [Fact]
        public async void SendAsync_PassingNullData()
        {
            var chainer = new Chainer(_broadcastImplMock.Object);
            await Assert.ThrowsAsync<ArgumentNullException>(() => chainer.SendAsync(null));
        }

        [Fact]
        public async void ReceiveAsync_CancelReceive()
        {
            var tokenSource = new CancellationTokenSource();
            tokenSource.Cancel();
            var chainer = new Chainer(_broadcastImplMock.Object);
            
            await Assert.ThrowsAsync<OperationCanceledException>(() => chainer.ReceiveAsync(tokenSource.Token));
        }

        [Fact]
        public async void ClearBufferAsync_CheckInnerCall()
        {
            var chainer = new Chainer(_broadcastImplMock.Object);
            await chainer.ClearBufferAsync();
            _broadcastImplMock.Verify(broadcast => broadcast.ClearBufferAsync(), Times.Once);
        }

        [Fact]
        public void Dispose_CheckInnerCall()
        {
            var chainer = new Chainer(_broadcastImplMock.Object);
            chainer.Dispose();
            _broadcastImplMock.Verify(broadcast => broadcast.Dispose(), Times.Once);
        }
    }
}