export const base = "https://api.nomis.cc/api/v1";

interface ISignatureProps {
  blockchainSlug: string;
  deadline: number;
  nonce: number;
  /** 0 - finance, 1 - eco */
  scoreType?: 1 | 0;
  wallet: `0x${string}`;
}

export const path = (props: ISignatureProps) => {
  const { blockchainSlug, deadline, nonce, scoreType = 0, wallet } = props;
  return `${base}/${blockchainSlug}/wallet/${wallet}/score?scoreType=${scoreType}&nonce=${nonce}&deadline=${deadline}`;
};
