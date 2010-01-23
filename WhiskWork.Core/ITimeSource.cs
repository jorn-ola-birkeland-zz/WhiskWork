using System;

namespace WhiskWork.Core
{
    public interface ITimeSource
    {
        DateTime GetTime();
    }
}
