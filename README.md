<p align="center">
   基于Csharp sdk 开发的区块链可视化事件流程状态机应用
</p>



## 场景构想

区块链的应用开发的主要对象目前还是以开发人员为主，即使简单的区块链交互上链动作，也需要开发人员有一定的区块链开发认知基础，并进行一定代码量的开发，才可以实现区块链应用的构建。


## 需求提炼：
1. 有没有一种工具或手段可以让开发人员、普通用户针对常规流程业务上链等需求，不需要编写代码，只需要简单操作，就可以完成一个区块链应用上链呢？
2. 一个多步骤的业务流程上链，在区块链系统是看不见、摸不着，大部分需要开发人员进行二次开发处理，才可以展示。有没有一种工具可以直观的查看区块链上流程，相比列表等更加形象的展示，如：图的交互。



## 视频教程

- 基于Csharp sdk 开发的区块链可视化事件流程状态机应用： <https://www.bilibili.com/video/BV17G4y1k74u/?vd_source=d13b0630d8f5bdd49b00820fee2bcbde>


## 实现方案



> （一） 技术组合


基于元磁之力开源框架中的快速开发框架YC.Boilerplate、结合FISCOBCOS Csharp Sdk进行组合开发，前端使用vue 技术栈，结合antv x6组件实现图形可视化。

> （二）实现思路

通过构建图形化流程设计，让普通人员也可以快速构建上链流程，在区块链上链存证等业务场景中，只需要选择流程中的对应行为进行对应数据填报，使用Csharp Sdk实现区块链的通信并进行上链操作，每次数据上链动作等都会和已上链数据进行比对校验，防止非法上链。

> （三）技术实现

1. 构建流程，采用系统流程设计器进行如下操作。

![image](https://github.com/FISCO-BCOS/BlockChain_ProcessVisual/blob/main/assets/images/1-%E6%B5%81%E7%A8%8B%E8%AE%BE%E8%AE%A1%E5%99%A8.png)

2.在流程管理可以查看创建流程
![image](https://gitee.com/yc-power/yc.boilerplate/raw/master/assets/images/YC.Micro%20%E5%BE%AE%E6%9C%8D%E5%8A%A1.jpg)

3. 在区块链存证示例中进行流程上链操作，业务录入人员需要流程行为和存证数据一一关联，就可以完成上链存证。其中：事务id为整个事件流程唯一id，同一个事件流程下，同一个业务行为只能上链一次，如果有另外需求需要进行二次开发。

3.1 存证操作
![image](https://gitee.com/yc-power/yc.boilerplate/raw/master/assets/images/YC.Micro%20%E5%BE%AE%E6%9C%8D%E5%8A%A1.jpg)

3.2 查看存证
![image](https://gitee.com/yc-power/yc.boilerplate/raw/master/assets/images/YC.Micro%20%E5%BE%AE%E6%9C%8D%E5%8A%A1.jpg)

3.3 webasefront 查看交易记录
![image](https://gitee.com/yc-power/yc.boilerplate/raw/master/assets/images/YC.Micro%20%E5%BE%AE%E6%9C%8D%E5%8A%A1.jpg)

3.4查看合约数据
![image](https://gitee.com/yc-power/yc.boilerplate/raw/master/assets/images/YC.Micro%20%E5%BE%AE%E6%9C%8D%E5%8A%A1.jpg)

3.5查看合约对应的存储数据
![image](https://gitee.com/yc-power/yc.boilerplate/raw/master/assets/images/YC.Micro%20%E5%BE%AE%E6%9C%8D%E5%8A%A1.jpg)

3.6 可视化查看业务流程行为存证
![image](https://gitee.com/yc-power/yc.boilerplate/raw/master/assets/images/YC.Micro%20%E5%BE%AE%E6%9C%8D%E5%8A%A1.jpg)

##  附录

>  操作流程

1. 将src\Db\traceevidencedb.sql 在mysql 数据库中创建
2. src\Backstage\BasicAppLayer\YC.ServiceWebApi\DefaultConfig.json 文件修改mysql 数据库信息
3. \src\Backstage\BlockChain\FISCOBCOS.CSharpSdk\Config\BaseConfig.cs 配置区块链rpc 地址
4. 将后端还原运行（nuget包还原）、前端运行(先还原 yarn install，之后运行：yarn run serve)，即可查看效果
  
备注：详细基础操作查看框架说明文档：http://doc.yc-l.com/#/quickStart



## 参与贡献

1. Fork 本仓库
2. 新建 Feat_xxx 分支
3. 提交代码
4. 新建 Pull Request


