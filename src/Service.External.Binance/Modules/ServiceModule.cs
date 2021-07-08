using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Autofac;
using Autofac.Core;
using Autofac.Core.Registration;
using Binance;
using MyJetWallet.Sdk.ExternalMarketsSettings.NoSql;
using MyJetWallet.Sdk.ExternalMarketsSettings.Services;
using MyJetWallet.Sdk.ExternalMarketsSettings.Settings;
using MyNoSqlServer.Abstractions;
using MyNoSqlServer.DataWriter;
using Service.External.Binance.Services;
using SimpleTrading.FeedTcpContext.TcpServer;

namespace Service.External.Binance.Modules
{
    public class ServiceModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var api = new BinanceApi();
            var user = new BinanceApiUser(Program.Settings.BinanceApiKey, Program.Settings.BinanceApiSecret);

            builder.RegisterInstance(api).AsSelf().SingleInstance();
            builder.RegisterInstance(user).AsSelf().SingleInstance();

            builder
                .RegisterType<MarketAndBalanceCache>()
                .AsSelf()
                .As<IStartable>()
                .AutoActivate()
                .SingleInstance();

            builder
                .RegisterType<OrderBookCacheManager>()
                .AsSelf()
                .As<IStartable>()
                .AutoActivate()
                .SingleInstance();

            builder
                .RegisterType<ExternalMarketSettingsManager>()
                .WithParameter("name", BinanceConst.Name)
                .As<IExternalMarketSettingsManager>()
                .As<IExternalMarketSettingsAccessor>()
                .AsSelf()
                .SingleInstance();


            RegisterMyNoSqlWriter<ExternalMarketSettingsNoSql>(builder, ExternalMarketSettingsNoSql.TableName);
        }

        private void RegisterMyNoSqlWriter<TEntity>(ContainerBuilder builder, string table)
            where TEntity : IMyNoSqlDbEntity, new()
        {
            builder.Register(ctx =>
                    new MyNoSqlServerDataWriter<TEntity>(
                        Program.ReloadedSettings(e => e.MyNoSqlWriterUrl), table, true))
                .As<IMyNoSqlServerDataWriter<TEntity>>()
                .SingleInstance();
        }
    }
}