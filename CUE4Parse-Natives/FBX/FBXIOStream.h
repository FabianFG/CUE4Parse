#include <fbxsdk.h>
#include <sstream>

// http://docs.autodesk.com/FBX/2014/ENU/FBX-SDK-Documentation/index.html?url=cpp_ref/class_fbx_stream.html,topicNumber=cpp_ref_class_fbx_stream_html2b5775d9-5d58-4231-a2a1-de97aada1fe6
class MyFbxStream : public FbxStream
{
public:
	MyFbxStream(FbxManager* manager) : manager(manager), currentPos(0) {}

	// Inherited via FbxStream
	EState GetState() override
	{
		return state;
	}

	bool Open(void* pStreamData) override
	{
		currentPos = 0;
		state = eOpen;
		return true;
	}

	bool Close() override
	{
		state = eClosed;
		return true;
	}

	bool Flush() override
	{
		return true;
	}

	size_t Write(const void* pData, FbxUInt64 pSize) override 
	{
		const std::streamoff begin = oss.tellp();
		oss.write(static_cast<const char*>(pData), static_cast<long long>(pSize));
		const std::streamoff end = oss.tellp();

		currentPos = end;
		return end - begin;
	}

	size_t Read(void*, FbxUInt64) const override
	{
		throw std::exception("not implemented");
		return size_t();
	}

	int GetReaderID() const override
	{
		return -1; //manager->GetIOPluginRegistry()->FindReaderIDByExtension("fbx");
	}

	int GetWriterID() const override
	{
		return manager->GetIOPluginRegistry()->FindWriterIDByExtension("fbx"); // FBX ascii (*.fbx)
	}

	void Seek(const FbxInt64& pOffset, const FbxFile::ESeekPos& pSeekPos) override
	{
		switch (pSeekPos) {

		case FbxFile::eBegin:
			currentPos = pOffset;
			break;

		case FbxFile::eCurrent:
			currentPos += pOffset;
			break;

		case FbxFile::eEnd:
			throw std::exception("not implemented");
			//currentPos = stream.size() - static_cast<long>(pOffset);
			break;
		}
		oss.seekp(currentPos);
	}

	FbxInt64 GetPosition() const override
	{
		return currentPos;
	}

	void SetPosition(FbxInt64 pPosition) override
	{
		oss.seekp(pPosition);
		currentPos = pPosition;
	}

	int GetError() const override
	{
		return oss.good() ? 0 : 1;
	}

	void ClearError() override
	{
		oss.clear(); // uhhhh?
	}

	TArray<char>* GetStreamData() const
	{
		// copy the stream data into a TArray
		const auto data = new TArray<char>();
		data->AddUninitialized(static_cast<int>(oss.str().size()));
		memcpy(data->GetData(), oss.rdbuf()->str().c_str(), oss.str().size());
		return data;
	}

private:
	FbxManager* manager;
	FbxInt64 currentPos;
	EState state;
	std::ostringstream oss;
};
