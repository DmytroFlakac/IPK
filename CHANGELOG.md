### Changelog

All notable changes to this project will be documented in this file. Dates are displayed in UTC.

#### Known Limitations
- `Thread.Sleep()` usage in both TCP and UDP client implementations may lead to unresponsive behavior under high load or slow network conditions, potentially affecting user experience.
- Limited validation on message formats and sizes might cause unexpected behavior when communicating with non-compliant servers or clients.
- Hardcoded retry logic in UDP client does not dynamically adjust to network conditions, which might result in lost messages under unstable network conditions.
- Blocking calls in TCP client (`stream.Read()`, `stream.Write()`) without timeout management could cause the application to hang if the server becomes unresponsive.
- The application's reliance on specific message formats without adequate error handling could lead to crashes or undefined behavior if malformed messages are received.
- Usage of synchronous network operations in main event loops (`TcpChatClient.StartInteractiveSession()`, `UdpChatClient.StartInteractiveSession()`) may impact scalability and responsiveness.


#### [Released](https://github.com/DmytroFlakac/IPK/compare/v1.0.0...HEAD)

### [v1.0.0](https://github.com/DmytroFlakac/IPK/compare/v0.1.0...v1.0.0)

> 1 April 2024

- 2024-04-01: diagram ([a7314c2](https://github.com/DmytroFlakac/IPK/commit/a7314c25aad509c4fef3376f2bf61db2068073a4))
- 2024-04-01: Add files via upload ([78fd1fd](https://github.com/DmytroFlakac/IPK/commit/78fd1fddea063d04cf943c4b30cbf7556d4d7ae0))
- 2024-04-01: Delete ipk-client-test-server-main directory ([413f09b](https://github.com/DmytroFlakac/IPK/commit/413f09b9544762830d0e0d1d89a8e4fcd9e489ff))
- 2024-04-01: Delete obj directory ([ee86152](https://github.com/DmytroFlakac/IPK/commit/ee86152265bc79dbecbd27a414f307924e32eaac))
- 2024-04-01: Delete bin directory ([8e0219c](https://github.com/DmytroFlakac/IPK/commit/8e0219cd523f716e298461fd598bb14697a613a4))
- 2024-04-01: Delete src directory ([548e265](https://github.com/DmytroFlakac/IPK/commit/548e2657ec80a6cc7d2100986a8800ccf94a7163))
- 2024-04-01: Delete Project directory ([ccf4711](https://github.com/DmytroFlakac/IPK/commit/ccf4711c3f9b4d5e2537eccae1bccede81f61c0a))
- 2024-04-01: Delete IPK directory ([570d68a](https://github.com/DmytroFlakac/IPK/commit/570d68a3c37df497f43faca00d412ddf58e9ea6e))
- 2024-04-01: project assembly ([d90284b](https://github.com/DmytroFlakac/IPK/commit/d90284bd57f60268f7bd99e2081a700457a28cab))
- 2024-04-01: project assembly ([2c23306](https://github.com/DmytroFlakac/IPK/commit/2c233066575fc8f656ff88a1814f7adea4b3f493))
- 2024-04-01: documentation ([a296c05](https://github.com/DmytroFlakac/IPK/commit/a296c052069617a256987c6ef82ecc6ad5a88c7c))
- 2024-04-01: finish ([cc3f3eb](https://github.com/DmytroFlakac/IPK/commit/cc3f3eb56cd3dd2d1ad8f5cc4bd5e95dfa7ff425))
- 2024-04-01: Create CHANGELOG.md ([9a59990](https://github.com/DmytroFlakac/IPK/commit/9a59990c429a6b2250c40c67605a8ea039ad5057))
- 2024-04-01: Update README.md ([dc0513d](https://github.com/DmytroFlakac/IPK/commit/dc0513db7859b6634bcd1bcbe071512fb601e84f))
- 2024-04-01: Create LICENSE ([590f93e](https://github.com/DmytroFlakac/IPK/commit/590f93e1ec2400f8cdc8577804d8ac3c305fe350))
- 2024-04-01: about to finish ([3a413f3](https://github.com/DmytroFlakac/IPK/commit/3a413f3bd4b93915cd227273e049ca765ee97a5a))
- 2024-03-31: finished, needs cleanig, refactoring and documentation ([0b4aa2a](https://github.com/DmytroFlakac/IPK/commit/0b4aa2a7abafd9947da31544ff29b06207d19daf))
- 2024-03-29: TCP finshed and wait for cleanig and refactoring ([8a940fa](https://github.com/DmytroFlakac/IPK/commit/8a940fa2ab0f1ad0b548e6cb9009b19477c24407))
- 2024-03-29: tests2 ([5f4213a](https://github.com/DmytroFlakac/IPK/commit/5f4213a45c5b5daf12a9b75289301f64485d8a73))
- 2024-03-29: tests ([ab84e60](https://github.com/DmytroFlakac/IPK/commit/ab84e606c4351eb4caa363f13822fd9d31173c92))
- 2024-03-29: pain ([05b9064](https://github.com/DmytroFlakac/IPK/commit/05b906452811ac22ef099329d1462938016fe41d))
- 2024-03-29: udp port troubles ([ed78af9](https://github.com/DmytroFlakac/IPK/commit/ed78af9e88532332b5096b4101e0bf4913718303))
- 2024-03-26: git test ([edb2982](https://github.com/DmytroFlakac/IPK/commit/edb2982e8194927b959c243992cf51dacacc8483))
- 2024-03-19: tcp client ([6de6cd9](https://github.com/DmytroFlakac/IPK/commit/6de6cd9c41e053e3ccdce0830f6f309368c7078c))
- 2024-03-17: Start dotnet boils ([e957a27](https://github.com/DmytroFlakac/IPK/commit/e957a27b152a27c170908c6a90006df0f91c1247))
- 2024-03-17: Start dotnet ([4b5d202](https://github.com/DmytroFlakac/IPK/commit/4b5d20262eb76bfeb6727b973f906fa0ee4df042))
- 2024-03-17: Start ([760b922](https://github.com/DmytroFlakac/IPK/commit/760b922f09b7d9119070f541709d8ef81f8cce2d))
- 2024-03-17: start ([7078f3d](https://github.com/DmytroFlakac/IPK/commit/7078f3d79154b0a535cee062b7d479b8d555d965))
- 2024-03-17: Initial commit ([f85a705](https://github.com/DmytroFlakac/IPK/commit/f85a70545839c7293e5be33dcfb0ec3f809384a0))