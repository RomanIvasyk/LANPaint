using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using LANPaint.Services.Broadcast;
using LANPaint.Services.Broadcast.UDP.Decorators;
using Moq;
using Xunit;

namespace LANPaint.UnitTests
{
    public class ChainerTest
    {
        private readonly Mock<IBroadcast> _broadcastMock;

        public ChainerTest()
        {
            _broadcastMock = new Mock<IBroadcast>();
            _broadcastMock.Setup(broadcast => broadcast.SendAsync(It.IsAny<byte[]>())).ReturnsAsync((byte[] dataParam) => dataParam.Length);
        }
        
        [Fact]
        public void LocalEndPoint_CheckInnerCall()
        {
            var chainer = new Chainer(_broadcastMock.Object);
            var endPoint = chainer.LocalEndPoint;
            _broadcastMock.Verify(broadcast => broadcast.LocalEndPoint, Times.Once);
        }
        
        //Does it even make sense to check this?
        [Fact]
        public async void SendAsync_CheckReturnValue()
        {
            const int dataLength = 10000;
            var data = new byte[dataLength];
            var chainer = new Chainer(_broadcastMock.Object);

            var result = await chainer.SendAsync(data);
            
            //We know that Chainer wraps data into Package, so sent amount of bytes
            //definitely will be more than initial payload size.
            Assert.True(result > dataLength);
        }

        [Fact]
        public async void SendReceiveAsync_CheckSendingReceivingOfTheSameData()
        {
            var storage = new Stack<byte[]>();
            var broadcastMock = new Mock<IBroadcast>();
            broadcastMock.Setup(broadcast => broadcast.SendAsync(It.IsAny<byte[]>())).Callback((byte[] dataParam) => storage.Push(dataParam))
                .ReturnsAsync((byte[] dataParam) => dataParam.Length);
            broadcastMock.Setup(broadcast => broadcast.ReceiveAsync(CancellationToken.None)).ReturnsAsync(() => storage.Pop());
            var broadcast = broadcastMock.Object;
            var data = RandomizeByteSequence(24000);

            await broadcast.SendAsync(data);
            var result = await broadcast.ReceiveAsync(CancellationToken.None);
            
            Assert.True(result.SequenceEqual(data));
            
            byte[] RandomizeByteSequence(int length)
            {
                var random = new Random();
                var sequence = new byte[length];
                
                for (var i = 0; i < length; i++)
                {
                    sequence[i] = (byte)random.Next(256);
                }

                return sequence;
            }
        }
        
        [Theory]
        [InlineData(10000, 8192, 2)]
        [InlineData(8192, 8192, 1)]
        [InlineData(20100, 10000, 3)]
        [InlineData(0, 8192, 0)]
        public async void SendAsync_CheckCountOfSendCalls(int dataLength, int maxSegmentLength,
            int expectedSendCallsNumber)
        {
            var data = new byte[dataLength];
            var chainer = new Chainer(_broadcastMock.Object, maxSegmentLength);
            await chainer.SendAsync(data);
            _broadcastMock.Verify(broadcast => broadcast.SendAsync(It.IsAny<byte[]>()), Times.Exactly(expectedSendCallsNumber));
        }

        [Fact]
        public async void SendAsync_PassingNullData()
        {
            var chainer = new Chainer(_broadcastMock.Object);
            await Assert.ThrowsAsync<ArgumentNullException>(() => chainer.SendAsync(null));
        }

        [Fact]
        public async void ClearBufferAsync_CheckInnerCall()
        {
            var chainer = new Chainer(_broadcastMock.Object);
            await chainer.ClearBufferAsync();
            _broadcastMock.Verify(broadcast => broadcast.ClearBufferAsync(), Times.Once);
        }
        
        [Fact]
        public void Dispose_CheckInnerCall()
        {
            var chainer = new Chainer(_broadcastMock.Object);
            chainer.Dispose();
            _broadcastMock.Verify(broadcast => broadcast.Dispose(), Times.Once);
        }
    }
}