using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using PlagiarismGuard.Data;
using PlagiarismGuard.Services;
using PlagiarismGuard.Models;
using System;
using System.Linq;
using System.Windows;
using Microsoft.Extensions.Configuration;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace PlagiarismGuard
{
    public partial class App : Application
    {
    }
}