using System.Threading;

namespace WoongConnector.Tools
{
    public sealed class LockFreeQueue<T> where T : class
    {
        private class SingleLinkNode
        {
            public SingleLinkNode Next;
            public T Item;
        }

        private SingleLinkNode mHead;
        private SingleLinkNode mTail;

        public LockFreeQueue()
        {
            mHead = new SingleLinkNode();
            mTail = mHead;
        }

        private static bool CompareAndExchange(ref SingleLinkNode pLocation, SingleLinkNode pComparand, SingleLinkNode pNewValue)
        {
            return pComparand == Interlocked.CompareExchange(ref pLocation, pNewValue, pComparand);
        }

        public T Next => mHead.Next?.Item;

        public void Enqueue(T pItem)
        {
            SingleLinkNode oldTail = null;
            SingleLinkNode newNode = new SingleLinkNode { Item = pItem };

            var newNodeWasAdded = false;

            while (!newNodeWasAdded)
            {
                oldTail = mTail;
                SingleLinkNode oldTailNext = oldTail.Next;

                if (mTail != oldTail) continue;

                if (oldTailNext == null)
                    newNodeWasAdded = CompareAndExchange(ref mTail.Next, null, newNode);

                else
                    CompareAndExchange(ref mTail, oldTail, oldTailNext);
            }

            CompareAndExchange(ref mTail, oldTail, newNode);
        }

        public bool Dequeue(out T pItem)
        {
            pItem = default(T);

            var haveAdvancedHead = false;

            while (!haveAdvancedHead)
            {

                SingleLinkNode oldHead = mHead;
                SingleLinkNode oldTail = mTail;
                SingleLinkNode oldHeadNext = oldHead.Next;

                if (oldHead != mHead) continue;

                if (oldHead == oldTail)
                {
                    if (oldHeadNext == null)
                        return false;

                    CompareAndExchange(ref mTail, oldTail, oldHeadNext);
                }

                else
                {
                    pItem = oldHeadNext.Item;
                    haveAdvancedHead = CompareAndExchange(ref mHead, oldHead, oldHeadNext);
                }
            }
            return true;
        }

        public T Dequeue()
        {
            Dequeue(out var result);
            return result;
        }
    }
}
